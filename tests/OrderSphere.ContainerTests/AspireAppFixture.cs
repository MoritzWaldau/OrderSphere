using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace OrderSphere.ContainerTests;

/// <summary>
/// Boots the real OrderSphere AppHost graph (Postgres, the Azure Service Bus emulator, Redis,
/// and every service) once per test run via <see cref="DistributedApplicationTestingBuilder"/>.
/// This is the second, slower test tier: it verifies what the in-process suite stubs — real
/// bus dispatch across queues, EF migrations applied at service startup, and cross-service
/// saga flows end to end. Requires Docker.
/// </summary>
public sealed class AspireAppFixture : IAsyncLifetime
{
    private DistributedApplication? _app;

    public DistributedApplication App => _app ?? throw new InvalidOperationException("App not started.");

    public async Task InitializeAsync()
    {
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.OrderSphere_AppHost>();

        // The AppHost declares secret/config parameters that are normally supplied via user-secrets
        // or Key Vault. Provide deterministic test values so the host starts. None of these are
        // exercised by the bus-level assertions below; payment-bypass-providers must be true so the
        // payment worker captures/refunds without a real provider.
        builder.Configuration["Parameters:bff-client-secret"] = "test-secret";
        builder.Configuration["Parameters:ordering-worker-secret"] = "test-secret";
        builder.Configuration["Parameters:notification-worker-secret"] = "test-secret";
        builder.Configuration["Parameters:payment-worker-secret"] = "test-secret";
        builder.Configuration["Parameters:payment-bypass-providers"] = "true";
        builder.Configuration["Parameters:oidc-authority"] = "https://ordersphere-dev.eu.auth0.com/";

        // Keep test output readable.
        builder.Services.AddLogging(logging => logging.SetMinimumLevel(LogLevel.Warning));

        _app = await builder.BuildAsync();
        await _app.StartAsync();

        // Wait for the resources the saga loop depends on. The Service Bus emulator and Postgres
        // are the slow movers; the workers must be running to consume.
        var ready = TimeSpan.FromMinutes(5);
        var notify = _app.Services.GetRequiredService<ResourceNotificationService>();
        foreach (var resource in new[]
                 {
                     "postgres", "azure-service-bus",
                     "ordersphere-catalog", "ordersphere-payment-worker",
                     "ordersphere-ordering-worker"
                 })
        {
            await notify.WaitForResourceAsync(resource, KnownResourceStates.Running)
                .WaitAsync(ready);
        }
    }

    public async Task<string> ConnectionStringAsync(string resourceName)
        => await App.GetConnectionStringAsync(resourceName)
           ?? throw new InvalidOperationException($"No connection string for '{resourceName}'.");

    public async Task DisposeAsync()
    {
        if (_app is not null)
            await _app.DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class AspireAppCollection : ICollectionFixture<AspireAppFixture>
{
    public const string Name = "aspire-app";
}
