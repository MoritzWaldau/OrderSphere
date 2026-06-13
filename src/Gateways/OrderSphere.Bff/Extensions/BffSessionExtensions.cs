using Microsoft.AspNetCore.DataProtection;

namespace OrderSphere.Bff.Extensions;

internal static class BffSessionExtensions
{
    /// <summary>
    /// Wires up Redis-backed DataProtection key ring and SignalR backplane.
    /// In the "Testing" environment both are ephemeral (no Redis needed).
    /// </summary>
    public static async Task AddBffSessionAsync(this WebApplicationBuilder builder)
    {
        if (!builder.Environment.IsEnvironment("Testing"))
        {
            var redis = await builder.AddOrderSphereRedisAsync("redis");

            builder.Services.AddDataProtection()
                .SetApplicationName("OrderSphere.Bff")
                .PersistKeysToStackExchangeRedis(redis, "DataProtection-Keys");

            builder.Services.AddSignalR()
                .AddStackExchangeRedis(options =>
                    options.ConnectionFactory = _ => Task.FromResult(redis));
        }
        else
        {
            builder.Services.AddDataProtection()
                .SetApplicationName("OrderSphere.Bff");
            builder.Services.AddSignalR();
        }
    }
}
