using System.Net;
using System.Net.Http.Headers;
using System.Text;
using AuthSeparation.Testing;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AuthSeparation.UserInfo.Tests;

/// <summary>
/// `AUTH-022` criterion 1: the stub compiles and serves every endpoint, returning
/// 501 at first. The cases are read from the contract, so an operation added to the
/// specification without regenerating the stub fails here rather than passing
/// unnoticed.
///
/// This is the contract that closes the slice (`ADR-0005`).
/// </summary>
public sealed class StubServesContractTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly Contract Spec = new("specs/auth-separation_userinfo-api_v1.yaml");

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
        // Carried from AUTH-021, where this guard caught a reader that found no path
        // parameters at all and failed silently: a URL containing a literal
        // "{consentKey}" still matches the route template "consents/{consentKey}",
        // so the theory above would have passed while testing nothing.
        var templated = Spec.Operations().Where(o => o.Path.Contains('{')).ToArray();

        Assert.Equal(2, templated.Length);
        Assert.All(templated, o => Assert.DoesNotContain('{', o.RequestPath));
    }

    [Fact]
    public void The_contract_declares_the_ten_operations_ADR_0006_measured()
    {
        // A guard on the guard: if the contract itself shrank, every case above
        // would still pass while testing less.
        Assert.Equal(10, Spec.Operations().Count());
    }

    [Fact]
    public async Task The_literal_users_me_route_wins_over_the_templated_one()
    {
        // This contract is the only one of the three with overlapping routes:
        // "users/me" and "users/{userId}" both match GET /v1/users/me, and NSwag
        // emits no route constraint that would separate them. ASP.NET Core prefers
        // the literal segment, so this resolves rather than throwing
        // AmbiguousMatchException — a 500 here, not a 501, would be the symptom.
        //
        // Worth pinning: nothing in the generated output states this, and it is
        // decided by routing precedence rather than by anything in the contract.
        var client = _factory.CreateClient();

        var response = await client.GetAsync(Spec.BasePath + "/users/me");

        Assert.Equal(HttpStatusCode.NotImplemented, response.StatusCode);
    }

    [Fact]
    public async Task A_method_the_contract_does_not_define_is_rejected()
    {
        // The templated route defines GET only. A DELETE against it must not fall
        // through to "users/me"'s delete, which is a different operation entirely
        // (account deletion). 405 is the router refusing, which is what we want.
        var client = _factory.CreateClient();

        var response = await client.DeleteAsync(Spec.BasePath + "/users/00000000-0000-0000-0000-000000000000");

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [Fact]
    public async Task A_path_outside_the_contract_is_not_served()
    {
        // The stub must serve the contract, not everything. "/profile" is the path
        // habit suggests; the contract says "/users/me".
        var client = _factory.CreateClient();

        var response = await client.GetAsync(Spec.BasePath + "/profile");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
