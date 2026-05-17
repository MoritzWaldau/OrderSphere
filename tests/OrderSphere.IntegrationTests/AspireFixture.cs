using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;
using OrderSphere.Application.Abstraction;

namespace OrderSphere.IntegrationTests;

public sealed class AspireFixture : IAsyncLifetime
{
    public DistributedApplication App { get; private set; } = default!;
    public CapturingEmailService Email { get; } = new();

    public async Task InitializeAsync()
    {
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.OrderSphere_AppHost>();

        // Swap real IEmailService for the in-memory capturing implementation in every host process.
        // Aspire's testing builder runs each AddProject<T> as a separate process; this hook lets us
        // intercept DI registration before the project starts.
        builder.Services.AddSingleton(Email);

        App = await builder.BuildAsync();
        await App.StartAsync();

        await App.ResourceNotifications
            .WaitForResourceHealthyAsync("ordersphere-ui");
        await App.ResourceNotifications
            .WaitForResourceHealthyAsync("ordersphere-worker");
    }

    public async Task DisposeAsync()
    {
        await App.StopAsync();
        await App.DisposeAsync();
    }
}

[CollectionDefinition(nameof(AspireCollection))]
public sealed class AspireCollection : ICollectionFixture<AspireFixture>
{
}
