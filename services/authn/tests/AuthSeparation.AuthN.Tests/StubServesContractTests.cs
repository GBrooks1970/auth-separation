using System.Net;
using System.Net.Http.Headers;
using System.Text;
using AuthSeparation.Testing;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AuthSeparation.AuthN.Tests;

/// <summary>
/// `AUTH-020` criterion 1: the stub compiles and serves every endpoint, returning
/// 501 at first. The cases are read from the contract, so an operation added to the
/// specification without regenerating the stub fails here rather than passing
/// unnoticed.
/// </summary>
public sealed class StubServesContractTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly Contract Spec = new("specs/auth-separation_authn-api_v1.yaml");

    private readonly WebApplicationFactory<Program> _factory;

    public StubServesContractTests(WebApplicationFactory<Program> factory) => _factory = factory;

    public static TheoryData<string, string> ContractOperations()
    {
        var data = new TheoryData<string, string>();
        foreach (var (method, _, requestPath) in Spec.Operations())
        {
            data.Add(method, requestPath);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(ContractOperations))]
    public async Task Every_contract_operation_is_routed_and_returns_501(string method, string path)
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(new HttpMethod(method), Spec.BasePath + path);

        // The contract's request bodies are all JSON. An empty object is enough to
        // reach the action: nothing is implemented behind it, so nothing validates.
        if (method is "POST" or "PUT" or "PATCH")
        {
            request.Content = new StringContent("{}", Encoding.UTF8);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        }

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotImplemented, response.StatusCode);
    }

    [Fact]
    public void The_contract_declares_the_twelve_operations_the_spike_measured()
    {
        // A guard on the guard: if the contract itself shrank, every case above
        // would still pass while testing less. ADR-0006 records 12 operations.
        Assert.Equal(12, Spec.Operations().Count());
    }

    [Fact]
    public async Task A_path_outside_the_contract_is_not_served()
    {
        // The stub must serve the contract, not everything. `/login` in particular:
        // it is the path habit suggests and the contract does not have (ADR-0006).
        var client = _factory.CreateClient();

        var response = await client.PostAsync(Spec.BasePath + "/login", new StringContent(string.Empty));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
