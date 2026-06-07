using OrderSphere.Mcp.Server.Gateway;

namespace OrderSphere.Mcp.Tests;

// Minimal ICallerContext stub so user-scoped tools can be unit-tested without
// pulling HttpContext into the test project.
internal sealed class FakeCaller(bool hasToken) : ICallerContext
{
    public bool HasBearerToken { get; } = hasToken;

    public static FakeCaller Authenticated { get; } = new(true);
    public static FakeCaller Anonymous { get; } = new(false);
}
