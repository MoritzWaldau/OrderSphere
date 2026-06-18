using FluentAssertions;
using Microsoft.Extensions.Configuration;
using OrderSphere.Ordering.Infrastructure.Shipping;
using Xunit;

namespace OrderSphere.IntegrationTests;

public sealed class FlatRateShippingProviderTests
{
    private static FlatRateShippingProvider Build(decimal flat, decimal threshold)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Shipping:FlatRate"] = flat.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["Shipping:FreeThreshold"] = threshold.ToString(System.Globalization.CultureInfo.InvariantCulture),
            })
            .Build();
        return new FlatRateShippingProvider(config);
    }

    [Fact]
    public void Below_threshold_charges_flat_rate()
    {
        var provider = Build(flat: 4.99m, threshold: 50m);

        provider.Calculate(49.99m).Should().Be(4.99m);
    }

    [Fact]
    public void At_or_above_threshold_is_free()
    {
        var provider = Build(flat: 4.99m, threshold: 50m);

        provider.Calculate(50m).Should().Be(0m);
        provider.Calculate(120m).Should().Be(0m);
    }

    [Fact]
    public void Defaults_apply_when_unconfigured()
    {
        var provider = new FlatRateShippingProvider(new ConfigurationBuilder().Build());

        provider.Calculate(10m).Should().Be(4.99m);  // default flat rate
        provider.Calculate(60m).Should().Be(0m);      // default free threshold 50
    }
}
