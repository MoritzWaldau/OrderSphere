namespace OrderSphere.Ordering.Domain.Services;

public static class TrackingNumberGenerator
{
    public static string Generate() =>
        $"OS-{DateTime.UtcNow.Year}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
}
