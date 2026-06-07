namespace OrderSphere.Mcp.Server.Gateway;

// Abstracts whether the current MCP call carries a caller bearer token. User-scoped
// tools use this to fail fast with a clear message when no token is present, instead
// of issuing a downstream call that would be rejected. Public catalog tools ignore it.
//
// Keeping this behind an interface lets the tools be unit-tested without depending on
// HttpContext directly.
public interface ICallerContext
{
    bool HasBearerToken { get; }
}

public sealed class HttpCallerContext(IHttpContextAccessor accessor) : ICallerContext
{
    public bool HasBearerToken
    {
        get
        {
            var authorization = accessor.HttpContext?.Request.Headers.Authorization.ToString();
            return !string.IsNullOrWhiteSpace(authorization);
        }
    }
}
