using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using OrderSphere.BuildingBlocks.EventBus.AzureServiceBus;
using OrderSphere.BuildingBlocks.EventBus.AzureServiceBus.Dlq;
using Xunit;

namespace OrderSphere.EventBus.AzureServiceBus.Tests.Dlq;

public sealed class AddDlqAdminTests
{
    [Fact]
    public void AddDlqAdmin_RegistersTheAdminSurfaceForTheGivenOwnedQueues()
    {
        var services = new ServiceCollection();

        services.AddDlqAdmin("orders", "payment-results");

        var provider = services.BuildServiceProvider();

        provider.GetRequiredService<DlqAdminOptions>().OwnedQueues.Should().Equal("orders", "payment-results");
        provider.GetRequiredService<DlqDepthCache>().Should().NotBeNull();
        services.Should().Contain(d => d.ServiceType == typeof(IDlqAdmin));
        services.Should().Contain(d => d.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService)
            && d.ImplementationType == typeof(DlqDepthMonitor));
    }
}
