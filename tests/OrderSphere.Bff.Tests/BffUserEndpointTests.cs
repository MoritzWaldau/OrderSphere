using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace OrderSphere.Bff.Tests;

/// <summary>
/// Integration tests for GET /bff/user.
/// Verifies the shape of the response and the XSRF cookie lifecycle
/// without requiring a live Keycloak or Redis instance.
/// </summary>
public sealed class BffUserEndpointTests(BffWebApplicationFactory factory)
    : IClassFixture<BffWebApplicationFactory>
{
    // ── Unauthenticated state ────────────────────────────────────────────────

    [Fact]
    public async Task Get_WhenUnauthenticated_Returns200()
    {
        var client = factory.CreateClient(NoRedirects());

        var response = await client.GetAsync("/bff/user");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_WhenUnauthenticated_ReturnsIsAuthenticatedFalse()
    {
        var client = factory.CreateClient(NoRedirects());

        var body = await GetUserBodyAsync(client);

        body.GetProperty("isAuthenticated").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task Get_WhenUnauthenticated_ReturnsNullSubAndEmail()
    {
        var client = factory.CreateClient(NoRedirects());

        var body = await GetUserBodyAsync(client);

        body.GetProperty("sub").ValueKind.Should().Be(JsonValueKind.Null);
        body.GetProperty("email").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task Get_WhenUnauthenticated_ReturnsEmptyRoles()
    {
        var client = factory.CreateClient(NoRedirects());

        var body = await GetUserBodyAsync(client);

        body.GetProperty("roles").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task Get_WhenUnauthenticated_IncludesXsrfToken()
    {
        var client = factory.CreateClient(NoRedirects());

        var body = await GetUserBodyAsync(client);

        // xsrfToken must be present and non-empty so the WASM client can
        // attach it as X-XSRF-TOKEN on mutating requests.
        var token = body.GetProperty("xsrfToken").GetString();
        token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Get_WhenUnauthenticated_SetsXsrfCookie()
    {
        var client = factory.CreateClient(NoRedirects());

        var response = await client.GetAsync("/bff/user");

        // The antiforgery middleware sets a non-HttpOnly XSRF-TOKEN cookie so the
        // browser can read it and the WASM client can include it in the request body.
        var setCookieHeaders = response.Headers
            .Where(h => h.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase))
            .SelectMany(h => h.Value)
            .ToList();

        setCookieHeaders.Should().Contain(h => h.StartsWith("XSRF-TOKEN="));
    }

    [Fact]
    public async Task Get_CalledTwice_ReturnsDifferentXsrfTokens()
    {
        // Each call must refresh the request token pair so the client always
        // has a current token even after long idle periods.
        var client = factory.CreateClient(NoRedirects());

        var body1 = await GetUserBodyAsync(client);
        var body2 = await GetUserBodyAsync(client);

        var token1 = body1.GetProperty("xsrfToken").GetString();
        var token2 = body2.GetProperty("xsrfToken").GetString();

        // Tokens are one-time-use by default; a second call issues a new pair.
        token1.Should().NotBe(token2);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static WebApplicationFactoryClientOptions NoRedirects() =>
        new() { AllowAutoRedirect = false };

    private static async Task<JsonElement> GetUserBodyAsync(HttpClient client)
    {
        var response = await client.GetAsync("/bff/user");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }
}
