using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// DelegatingHandler that acquires and caches a client-credentials access token from
/// Keycloak, then attaches it as a Bearer token to every outgoing request.
///
/// Configuration keys (resolved at runtime):
///   Keycloak:Authority      — realm base URL, e.g. http://keycloak:8080/realms/ordersphere
///   Keycloak:ClientId       — the service-account client ID, e.g. ordering-worker
///   Keycloak:ClientSecret   — the corresponding client secret
///
/// Token caching:
///   The acquired token is cached in-memory for the lifetime of this handler instance.
///   A SemaphoreSlim prevents concurrent token fetches when the cached token is stale.
///   The cache is intentionally conservative: tokens are renewed 30 seconds before
///   actual expiry so downstream services never receive a token that is about to expire.
///
/// Handler lifetime:
///   Register as Transient (standard for DelegatingHandler in IHttpClientFactory).
///   IHttpClientFactory rotates the handler pipeline every ~2 minutes by default, which
///   results in at most one token fetch per rotation window per service — negligible load.
/// </summary>
public sealed class ClientCredentialsTokenHandler(
    IConfiguration config,
    IHttpClientFactory httpClientFactory,
    ILogger<ClientCredentialsTokenHandler> logger) : DelegatingHandler
{
    private string? _cachedToken;
    private DateTimeOffset _tokenExpiry = DateTimeOffset.MinValue;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await GetAccessTokenAsync(cancellationToken);
        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await base.SendAsync(request, cancellationToken);
    }

    private async Task<string?> GetAccessTokenAsync(CancellationToken ct)
    {
        // Fast path — cached token still valid.
        if (_cachedToken is not null && DateTimeOffset.UtcNow < _tokenExpiry)
            return _cachedToken;

        await _semaphore.WaitAsync(ct);
        try
        {
            // Re-check after acquiring the lock (another thread may have refreshed already).
            if (_cachedToken is not null && DateTimeOffset.UtcNow < _tokenExpiry)
                return _cachedToken;

            var authority = config["Keycloak:Authority"]
                ?? throw new InvalidOperationException("Keycloak:Authority is not configured.");
            var clientId = config["Keycloak:ClientId"]
                ?? throw new InvalidOperationException("Keycloak:ClientId is not configured.");
            var clientSecret = config["Keycloak:ClientSecret"]
                ?? throw new InvalidOperationException("Keycloak:ClientSecret is not configured.");

            var tokenEndpoint = $"{authority.TrimEnd('/')}/protocol/openid-connect/token";

            // Use the dedicated bare HTTP client to avoid recursive handler chains.
            var client = httpClientFactory.CreateClient("keycloak-cc-token");
            using var body = new FormUrlEncodedContent(
            [
                new KeyValuePair<string, string>("grant_type",    "client_credentials"),
                new KeyValuePair<string, string>("client_id",     clientId),
                new KeyValuePair<string, string>("client_secret", clientSecret),
            ]);

            var response = await client.PostAsync(tokenEndpoint, body, ct);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                logger.LogWarning(
                    "Client credentials token request failed for {ClientId} ({StatusCode}): {Error}",
                    clientId, (int)response.StatusCode, error);
                return null;
            }

            var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
            var accessToken = json.GetProperty("access_token").GetString()!;
            var expiresIn = json.GetProperty("expires_in").GetInt32();

            _cachedToken = accessToken;
            _tokenExpiry = DateTimeOffset.UtcNow.AddSeconds(expiresIn - 30);

            logger.LogDebug(
                "Fetched client_credentials token for {ClientId}; valid for {ExpiresIn}s.",
                clientId, expiresIn);

            return _cachedToken;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}

/// <summary>
/// Extension methods for wiring <see cref="ClientCredentialsTokenHandler"/> into an
/// <see cref="IHttpClientBuilder"/> pipeline.
/// </summary>
public static class ClientCredentialsExtensions
{
    /// <summary>
    /// Registers <see cref="ClientCredentialsTokenHandler"/> as a transient service and
    /// adds it to the named/typed HTTP client's handler pipeline.
    ///
    /// The service must configure <c>Keycloak:Authority</c>, <c>Keycloak:ClientId</c>, and
    /// <c>Keycloak:ClientSecret</c> — typically injected by Aspire via environment variables.
    ///
    /// A dedicated bare HTTP client named <c>keycloak-cc-token</c> is registered to avoid
    /// recursive handler-pipeline resolution when the handler calls the Keycloak token endpoint.
    /// </summary>
    public static IHttpClientBuilder AddClientCredentialsHandler(
        this IHttpClientBuilder builder)
    {
        // Bare client for the token endpoint — must not carry ClientCredentialsTokenHandler itself.
        builder.Services.AddHttpClient("keycloak-cc-token");

        builder.Services.AddTransient<ClientCredentialsTokenHandler>();
        builder.AddHttpMessageHandler<ClientCredentialsTokenHandler>();

        return builder;
    }
}
