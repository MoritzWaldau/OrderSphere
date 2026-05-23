using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace OrderSphere.Bff.Tests;

/// <summary>
/// WebApplicationFactory for BFF integration tests.
/// Replaces all external dependencies (Redis, Keycloak) with in-process stubs
/// so tests run without any running infrastructure.
/// </summary>
public sealed class BffWebApplicationFactory : WebApplicationFactory<Program>
{
    /// <summary>
    /// HMAC-SHA256 key shared between token creation helpers and the mock OIDC config.
    /// All back-channel logout JWTs in tests are signed with this key.
    /// A non-empty KeyId is required so JsonWebTokenHandler includes the "kid" header
    /// parameter and validation can match the token's kid to this key.
    /// </summary>
    public static readonly SymmetricSecurityKey TestSigningKey =
        new(Convert.FromBase64String("dGVzdC1zaWduaW5nLWtleS1mb3ItYmZmLXVuaXQtdGVzdHMtMzI="))
        {
            KeyId = "bff-test-signing-key-1",
        };

    public const string FakeAuthority = "https://fake-keycloak.test/realms/ordersphere";
    public const string FakeClientId  = "web-bff";

    /// <summary>Static OIDC configuration returned by the mock IConfigurationManager.</summary>
    public static readonly OpenIdConnectConfiguration TestOidcConfig;

    static BffWebApplicationFactory()
    {
        TestOidcConfig = new OpenIdConnectConfiguration
        {
            Issuer = FakeAuthority,
            // Required so the OIDC challenge handler can redirect unauthenticated
            // requests to this URL instead of throwing InvalidOperationException.
            AuthorizationEndpoint = FakeAuthority + "/protocol/openid-connect/auth",
            // Required so the OIDC sign-out handler can redirect instead of throwing
            // when POST /bff/logout passes the CSRF filter in integration tests.
            EndSessionEndpoint = FakeAuthority + "/protocol/openid-connect/logout",
        };
        TestOidcConfig.SigningKeys.Add(TestSigningKey);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set environment to "Testing".
        // This causes Program.cs to skip Redis DataProtection (ephemeral key ring)
        // and triggers loading of appsettings.Testing.json from the BFF project,
        // which provides the fake Keycloak and Redis configuration values.
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Replace Redis-backed IDistributedCache with an in-memory implementation
            // so RedisTicketStore works without a running Redis instance.
            services.RemoveAll<IDistributedCache>();
            services.AddDistributedMemoryCache();

            // Replace the singleton IConfigurationManager that BackchannelLogoutEndpoint
            // uses for logout_token JWT validation.
            services.RemoveAll<IConfigurationManager<OpenIdConnectConfiguration>>();
            services.AddSingleton<IConfigurationManager<OpenIdConnectConfiguration>>(
                new StaticOidcConfigurationManager(TestOidcConfig));
        });

        // Override OIDC options to prevent the OpenIdConnect middleware's own
        // internal document retriever from trying to reach fake-keycloak.test.
        // Using PostConfigure so it runs after Program.cs registers the options.
        builder.ConfigureServices(services =>
        {
            services.PostConfigure<OpenIdConnectOptions>(
                OpenIdConnectDefaults.AuthenticationScheme,
                options =>
                {
                    options.ConfigurationManager = new StaticOidcConfigurationManager(TestOidcConfig);
                    options.RequireHttpsMetadata = false;
                });
        });
    }
}

/// <summary>
/// IConfigurationManager implementation that always returns a fixed
/// <see cref="OpenIdConnectConfiguration"/> without any network calls.
/// </summary>
internal sealed class StaticOidcConfigurationManager(OpenIdConnectConfiguration config)
    : IConfigurationManager<OpenIdConnectConfiguration>
{
    public Task<OpenIdConnectConfiguration> GetConfigurationAsync(CancellationToken cancel)
        => Task.FromResult(config);

    public void RequestRefresh() { }
}
