using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace OrderSphere.ContainerTests;

/// <summary>
/// Boots the full Aspire AppHost graph once per test collection and exposes
/// helper methods for connecting to provisioned resources.
/// </summary>
public sealed class AspireAppFixture : IAsyncLifetime
{
    private DistributedApplication? _app;

    public DistributedApplication App => _app
        ?? throw new InvalidOperationException("Fixture not initialised.");

    public async Task InitializeAsync()
    {
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.OrderSphere_AppHost>();

        // Bypass real secrets; test infra uses emulators / containers.
        builder.Configuration["Parameters:payment-bypass-providers"] = "true";
        builder.Configuration["Parameters:bff-client-secret"] = "test-secret";
        builder.Configuration["Parameters:ordering-worker-secret"] = "test-secret";
        builder.Configuration["Parameters:notification-worker-secret"] = "test-secret";
        builder.Configuration["Parameters:payment-worker-secret"] = "test-secret";
        builder.Configuration["Parameters:oidc-authority"] = "https://test.auth0.invalid";

        _app = await builder.BuildAsync();
        await _app.StartAsync();

        var notify = _app.Services.GetRequiredService<ResourceNotificationService>();

        // Wait for the resources that container lock tests depend on.
        await notify
            .WaitForResourceAsync("redis", KnownResourceStates.Running)
            .WaitAsync(TimeSpan.FromMinutes(5));
    }

    public async Task<string> ConnectionStringAsync(string resourceName)
        => await App.GetConnectionStringAsync(resourceName)
           ?? throw new InvalidOperationException(
               $"Resource '{resourceName}' did not expose a connection string.");

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
