using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using OrderSphere.Payment.Infrastructure;
using OrderSphere.Payment.Infrastructure.Providers;
using Stripe;
using Xunit;

namespace OrderSphere.Payment.Tests;

/// <summary>
/// B1 — the Stripe provider registers under the "CreditCard" method name so existing checkout
/// routes to it without a UI contract change, but only when an API key is configured. Without a
/// key the simulated provider stays active so local development needs no external credentials.
/// </summary>
public sealed class StripePaymentProviderTests
{
    [Fact]
    public void MethodName_IsCreditCard()
    {
        var provider = new StripePaymentProvider(
            Substitute.For<IStripeClient>(),
            NullLogger<StripePaymentProvider>.Instance);

        provider.MethodName.Should().Be("CreditCard");
    }

    [Fact]
    public void WhenStripeApiKeyConfigured_CreditCardResolvesToStripe()
    {
        using var sp = BuildInfrastructure(new() { ["Stripe:ApiKey"] = "sk_test_dummy" });

        var factory = sp.GetRequiredService<IPaymentProviderFactory>();

        factory.GetProvider("CreditCard").Should().BeOfType<StripePaymentProvider>();
    }

    [Fact]
    public void WhenStripeApiKeyMissing_CreditCardResolvesToSimulatedProvider()
    {
        using var sp = BuildInfrastructure(new());

        var factory = sp.GetRequiredService<IPaymentProviderFactory>();

        factory.GetProvider("CreditCard").Should().BeOfType<CreditCardPaymentProvider>();
    }

    private static ServiceProvider BuildInfrastructure(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddPaymentInfrastructure(configuration);
        return services.BuildServiceProvider();
    }
}
