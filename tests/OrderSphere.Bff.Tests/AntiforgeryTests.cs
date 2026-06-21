using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace OrderSphere.Bff.Tests;

/// <summary>
/// Integration tests for anti-forgery protection on BFF endpoints.
///
/// Two CSRF mechanisms are in play:
///
/// 1. <c>AntiforgeryEndpointFilter</c> on <c>POST /bff/logout</c>:
///    Validates the X-XSRF-TOKEN header before executing the endpoint.
///    This is testable without an authenticated session.
///
/// 2. Generic CSRF middleware before <c>MapReverseProxy</c>:
///    Validates X-XSRF-TOKEN for all non-safe mutations to <c>/api/**</c>.
///    This middleware is only reached AFTER <see cref="UseAuthorization"/> passes,
///    which requires a valid authenticated session (BffUserPolicy).
///    Authenticated-session tests are therefore deferred; they require a mock
///    sign-in endpoint not yet available in the test factory.
/// </summary>
public sealed class AntiforgeryTests(BffWebApplicationFactory factory)
    : IClassFixture<BffWebApplicationFactory>
{
    // These tests exercise the endpoint-level filter and are independent of the
    // /api/** reverse-proxy CSRF middleware.

    [Fact]
    public async Task Post_ToBffLogout_WithoutXsrfCookie_Returns403()
    {
        // No prior call to /bff/user means no XSRF-TOKEN cookie is set.
        // The AntiforgeryEndpointFilter cannot validate the request → 403.
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });

        var response = await client.PostAsync("/bff/logout", new StringContent(string.Empty));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Post_ToBffLogout_WithoutXsrfCookie_ReturnsCsrfProblemBody()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });

        var response = await client.PostAsync("/bff/logout", new StringContent(string.Empty));

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        // AntiforgeryEndpointFilter returns Results.Problem with this title.
        body.GetProperty("title").GetString().Should().Contain("CSRF token validation failed");
    }

    [Fact]
    public async Task Post_ToBffLogout_WithValidXsrfToken_PassesEndpointFilter()
    {
        // Obtain the XSRF cookie and request token from /bff/user.
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,   // cookie jar persists XSRF-TOKEN cookie
        });

        var userBody = await (await client.GetAsync("/bff/user")).Content.ReadFromJsonAsync<JsonElement>();
        var xsrfToken = userBody.GetProperty("xsrfToken").GetString()!;

        var request = new HttpRequestMessage(HttpMethod.Post, "/bff/logout")
        {
            Content = new StringContent(string.Empty),
        };
        request.Headers.Add("X-XSRF-TOKEN", xsrfToken);

        var response = await client.SendAsync(request);

        // The endpoint filter passes; the sign-out handler redirects to "/" or Auth0.
        // What matters is that the response is NOT a CSRF 403.
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }


    [Fact]
    public async Task Get_ToApi_IsNotBlockedByCsrfMiddleware()
    {
        // CSRF middleware only runs for non-safe methods (POST, PUT, DELETE, …).
        // A GET to /api/** reaches UseAuthorization first (unauthenticated → 401),
        // but the response must not be 403 due to CSRF.
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });

        var response = await client.GetAsync("/api/orders");

        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden,
            because: "GET is a safe method; the CSRF middleware must not reject it");
    }


    [Fact]
    public async Task Post_ToApi_UnauthenticatedRequest_GetsAuthChallenge_NotCsrf403()
    {
        // UseAuthorization short-circuits the pipeline for unauthenticated requests
        // to /api/** (which require BffUserPolicy) BEFORE the CSRF middleware runs.
        // For /api/** the OIDC OnRedirectToIdentityProvider handler converts the
        // would-be 302 challenge into a 401 so a browser fetch sees a clear
        // "not signed in" status instead of an opaque cross-origin redirect (CORS) to Auth0.
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });

        var response = await client.PostAsync("/api/orders",
            new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));

        // Unauthenticated → auth challenge surfaced as 401 (not a 302 redirect to
        // Auth0). The key assertion is that it is NOT 403 (which would indicate
        // incorrect CSRF rejection of a request that never passed authentication).
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            because: "the BFF returns 401 for unauthenticated /api/** mutations instead of an OIDC redirect");
    }


    [Fact]
    public async Task CsrfErrorBodies_AreDistinguishable()
    {
        // The AntiforgeryEndpointFilter (for /bff/logout) returns a ProblemDetails body
        // with title "CSRF token validation failed.", while the generic CSRF middleware
        // (for /api/**) returns { error: "CSRF validation failed." }.
        // This test verifies the endpoint-filter format so future authenticated-session
        // tests can tell the two apart.
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });

        var response = await client.PostAsync("/bff/logout", new StringContent(string.Empty));
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        // ProblemDetails format: contains "title", NOT the middleware's "error" key.
        body.TryGetProperty("title", out _).Should().BeTrue("endpoint filter uses ProblemDetails");
        body.TryGetProperty("error", out _).Should().BeFalse("middleware-style 'error' key must not appear here");
    }
}
