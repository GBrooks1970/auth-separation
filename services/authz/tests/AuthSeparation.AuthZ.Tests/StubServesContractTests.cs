using System.Net;
using System.Net.Http.Headers;
using System.Text;
using AuthSeparation.Testing;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AuthSeparation.AuthZ.Tests;

/// <summary>
/// `AUTH-021` criterion 1: the stub compiles and serves every endpoint, returning
/// 501 at first. The cases are read from the contract, so an operation added to the
/// specification without regenerating the stub fails here rather than passing
/// unnoticed.
///
/// Unlike AuthN, this contract has **templated paths** — `/roles/{roleKey}`,
/// `/users/{userId}/roles` and `/users/{userId}/roles/{roleKey}`. The shared
/// <see cref="Contract"/> fills them from the parameter schemas, which matters for
/// `userId`: it generates as a `System.Guid`, so a segment that does not parse as
/// one never reaches the action at all.
/// </summary>
public sealed class StubServesContractTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly Contract Spec = new("specs/auth-separation_authz-api_v1.yaml");

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

        if (method is "POST" or "PUT" or "PATCH")
        {
            request.Content = new StringContent("{}", Encoding.UTF8);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        }

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotImplemented, response.StatusCode);
    }

    [Fact]
    public void Every_templated_path_is_actually_exercised()
    {
        // Without this, a bug that quietly dropped the templated operations would
        // leave the theory above passing on a smaller set. Three of the nine paths
        // carry parameters, and they are the shape AuthN never tested — but they
        // carry six of the fourteen operations between them, which is the number
        // that matters here.
        var templated = Spec.Operations().Where(o => o.Path.Contains('{')).ToArray();

        Assert.Equal(6, templated.Length);
        Assert.All(templated, o => Assert.DoesNotContain('{', o.RequestPath));
    }

    [Fact]
    public void The_contract_declares_the_fourteen_operations_ADR_0006_measured()
    {
        // A guard on the guard: if the contract itself shrank, every case above
        // would still pass while testing less.
        Assert.Equal(14, Spec.Operations().Count());
    }

    [Fact]
    public async Task A_path_outside_the_contract_is_not_served()
    {
        // The stub must serve the contract, not everything. `/check` is the path
        // habit suggests for a decision endpoint; the contract says
        // `/decisions/check`.
        var client = _factory.CreateClient();

        var response = await client.PostAsync(Spec.BasePath + "/check", new StringContent(string.Empty));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task A_path_parameter_of_the_wrong_type_still_reaches_the_stub()
    {
        // Pinning a real and slightly surprising property of the generated stub.
        // `userId` generates as a `System.Guid`, so it is tempting to assume the
        // router rejects a segment that is not one. It does not: NSwag emits
        // `Route("users/{userId}/roles")` with **no `:guid` constraint**, and the
        // controller carries no `[ApiController]` attribute, so the automatic
        // 400-on-invalid-model behaviour is not in play either. The segment binds
        // to `default(Guid)` and the action runs.
        //
        // That is correct for this slice, not a defect. Rejecting it would be
        // request validation, which `ADR-0005` puts out of scope along with
        // everything else behind the stub — and adding `[ApiController]` to buy it
        // would change what the stub does rather than what it validates. Asserted
        // so that if a future decision changes it, the change is deliberate.
        var client = _factory.CreateClient();

        var response = await client.GetAsync(Spec.BasePath + "/users/not-a-guid/roles");

        Assert.Equal(HttpStatusCode.NotImplemented, response.StatusCode);
    }
}
