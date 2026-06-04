using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Shared Redis wiring for OrderSphere services.
/// </summary>
public static class RedisExtensions
{
    /// <summary>
    /// Connects a single <see cref="IConnectionMultiplexer"/> (registered as a singleton) and a
    /// matching <c>IDistributedCache</c> backed by it.
    /// <para>
    /// Against Azure Managed Redis / Azure Cache for Redis the endpoint enforces Microsoft Entra ID
    /// authentication and the Aspire-injected connection string carries no password; a raw
    /// connection therefore fails with <c>NOAUTH</c>. This method authenticates with the
    /// application's managed identity and lets the StackExchange.Redis Azure extension refresh the
    /// access token for the lifetime of the connection. Against a local Redis (Aspire dev container)
    /// it connects without credentials.
    /// </para>
    /// The multiplexer is connected eagerly so that synchronous consumers — notably ASP.NET Core
    /// Data Protection's Redis key repository — have an authenticated connection available on first
    /// use rather than failing mid-request.
    /// </summary>
    /// <returns>The connected multiplexer, for callers that need it directly (e.g. Data Protection,
    /// SignalR backplane).</returns>
    public static async Task<IConnectionMultiplexer> AddOrderSphereRedisAsync(
        this IHostApplicationBuilder builder,
        string connectionName = "redis")
    {
        var connectionString = builder.Configuration.GetConnectionString(connectionName)
            ?? throw new InvalidOperationException(
                $"Redis connection string '{connectionName}' is not configured. " +
                "Ensure the Aspire Redis resource is referenced by this project.");

        var options = ConfigurationOptions.Parse(connectionString);

        // Only acquire an Entra token when the target is an Azure Redis endpoint that was provisioned
        // without an access key. A local dev container (or an access-key connection string) connects
        // unchanged.
        if (string.IsNullOrEmpty(options.Password) && options.EndPoints.Any(IsAzureRedisEndpoint))
        {
            // AZURE_CLIENT_ID is set by Azure Container Apps for the user-assigned managed identity.
            var managedIdentityClientId = builder.Configuration["AZURE_CLIENT_ID"];
            if (!string.IsNullOrEmpty(managedIdentityClientId))
            {
                await options.ConfigureForAzureWithUserAssignedManagedIdentityAsync(managedIdentityClientId);
            }
            else
            {
                await options.ConfigureForAzureWithSystemAssignedManagedIdentityAsync();
            }
        }

        IConnectionMultiplexer multiplexer = await ConnectionMultiplexer.ConnectAsync(options);

        builder.Services.AddSingleton(multiplexer);
        builder.Services.AddStackExchangeRedisCache(cache =>
            cache.ConnectionMultiplexerFactory = () => Task.FromResult(multiplexer));

        return multiplexer;
    }

    private static bool IsAzureRedisEndpoint(EndPoint endpoint) =>
        endpoint is DnsEndPoint dns
        && (dns.Host.EndsWith(".redis.azure.net", StringComparison.OrdinalIgnoreCase)
            || dns.Host.EndsWith(".redis.cache.windows.net", StringComparison.OrdinalIgnoreCase));
}
