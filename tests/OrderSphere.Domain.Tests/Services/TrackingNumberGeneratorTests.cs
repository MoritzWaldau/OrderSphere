using OrderSphere.Domain.Services;

namespace OrderSphere.Domain.Tests.Services;

public class TrackingNumberGeneratorTests
{
    [Fact]
    public void Generate_ProducesExpectedFormat()
    {
        var tracking = TrackingNumberGenerator.Generate();

        tracking.Should().MatchRegex(@"^OS-\d{4}-[A-F0-9]{8}$");
    }

    [Fact]
    public void Generate_ContainsCurrentYear()
    {
        var tracking = TrackingNumberGenerator.Generate();

        tracking.Should().Contain($"OS-{DateTime.UtcNow.Year}-");
    }

    [Fact]
    public void Generate_TwoCallsProduceDifferentNumbers()
    {
        var a = TrackingNumberGenerator.Generate();
        var b = TrackingNumberGenerator.Generate();

        a.Should().NotBe(b);
    }
}
