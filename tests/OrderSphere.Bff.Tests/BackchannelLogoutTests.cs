using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace OrderSphere.Bff.Tests;

/// <summary>
/// Integration tests for POST /bff/backchannel-logout.
/// The endpoint validates the Auth0 logout_token JWT, then revokes the matching
/// Redis session.  In tests, both OIDC config and session storage are in-process stubs.
/// </summary>
public sealed class BackchannelLogoutTests(BffWebApplicationFactory factory)
    : IClassFixture<BffWebApplicationFactory>
{
    private const string BackchannelLogoutEvent =
        "http://schemas.openid.net/event/backchannel-logout";

    // ── Request format validation ────────────────────────────────────────────

    [Fact]
    public async Task Post_WithoutFormContentType_Returns400()
    {
        var client = factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Post, "/bff/backchannel-logout")
        {
            Content = new StringContent("logout_token=whatever"),
        };
        // Default content-type is text/plain, not application/x-www-form-urlencoded.

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_WithFormButMissingLogoutToken_Returns400()
    {
        var client = factory.CreateClient();

        var form = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("other_param", "value"),
        });

        var response = await client.PostAsync("/bff/backchannel-logout", form);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── JWT signature validation ──────────────────────────────────────────────

    [Fact]
    public async Task Post_WithTamperedToken_Returns400()
    {
        var client = factory.CreateClient();
        var validJwt = CreateLogoutToken();
        var tampered = validJwt[..^4] + "XXXX";  // corrupt the signature

        var response = await PostLogoutTokenAsync(client, tampered);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_WithWrongSigningKey_Returns400()
    {
        var wrongKey = new SymmetricSecurityKey(new byte[64]);  // all-zero key ≠ TestSigningKey
        var jwt = CreateLogoutToken(signingKey: wrongKey);
        var client = factory.CreateClient();

        var response = await PostLogoutTokenAsync(client, jwt);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── OIDC spec claim validation ────────────────────────────────────────────

    [Fact]
    public async Task Post_WithNonceClaim_Returns400()
    {
        var jwt = CreateLogoutToken(includeNonce: true);
        var client = factory.CreateClient();

        var response = await PostLogoutTokenAsync(client, jwt);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_WithMissingEventsClaim_Returns400()
    {
        var jwt = CreateLogoutToken(includeEvents: false);
        var client = factory.CreateClient();

        var response = await PostLogoutTokenAsync(client, jwt);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_WithMissingSidClaim_Returns200()
    {
        // When sid is absent, the spec says the IdP MUST provide sub or sid.
        // The endpoint returns 200 so Auth0 does not retry, but cannot revoke.
        var jwt = CreateLogoutToken(sid: null);
        var client = factory.CreateClient();

        var response = await PostLogoutTokenAsync(client, jwt);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── Session revocation ────────────────────────────────────────────────────

    [Fact]
    public async Task Post_WithValidToken_NoMatchingSession_Returns200()
    {
        // A valid logout_token for a session that is not in the cache (already expired
        // or already logged out) must return 200 — not an error.
        var jwt = CreateLogoutToken(sid: "non-existent-session-id");
        var client = factory.CreateClient();

        var response = await PostLogoutTokenAsync(client, jwt);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Post_WithValidToken_MatchingSession_Returns200AndRemovesSession()
    {
        const string testSid = "auth0-session-abc";
        const string sessionKey = "bff:session:test-key-for-revocation";
        const string sidIndexKey = $"bff:sid:{testSid}";

        // Pre-populate the in-memory cache to simulate an active session.
        var cache = factory.Services.GetRequiredService<IDistributedCache>();
        await cache.SetStringAsync(sidIndexKey, sessionKey);
        await cache.SetStringAsync(sessionKey, "serialized-ticket-placeholder");

        var jwt = CreateLogoutToken(sid: testSid);
        var client = factory.CreateClient();

        var response = await PostLogoutTokenAsync(client, jwt);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // The session key must have been removed from the cache.
        var remaining = await cache.GetStringAsync(sessionKey);
        remaining.Should().BeNull("the back-channel logout endpoint must revoke the session");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static Task<HttpResponseMessage> PostLogoutTokenAsync(
        HttpClient client, string logoutToken)
    {
        var form = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("logout_token", logoutToken),
        });
        return client.PostAsync("/bff/backchannel-logout", form);
    }

    /// <summary>
    /// Creates a signed logout_token JWT using the test signing key (HMAC-SHA256).
    /// Parameters allow individual tests to exercise specific validation branches.
    /// </summary>
    private static string CreateLogoutToken(
        string issuer = BffWebApplicationFactory.FakeAuthority,
        string audience = BffWebApplicationFactory.FakeClientId,
        string? sid = "test-session-id-001",
        string? sub = "user-sub-0000",
        bool includeNonce = false,
        bool includeEvents = true,
        SymmetricSecurityKey? signingKey = null)
    {
        var key = signingKey ?? BffWebApplicationFactory.TestSigningKey;
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new Dictionary<string, object>
        {
            ["iat"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
        };

        if (sub is not null)
            claims["sub"] = sub;

        if (sid is not null)
            claims["sid"] = sid;

        if (includeNonce)
            claims["nonce"] = "nonce-value-that-must-not-be-present";

        if (includeEvents)
            claims["events"] = new Dictionary<string, object>
            {
                // JsonWebTokenHandler requires a serializable type; empty dict → "{}".
                [BackchannelLogoutEvent] = new Dictionary<string, object>(),
            };

        var handler = new JsonWebTokenHandler();
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = audience,
            Claims = claims,
            SigningCredentials = creds,
        };

        return handler.CreateToken(descriptor);
    }
}
