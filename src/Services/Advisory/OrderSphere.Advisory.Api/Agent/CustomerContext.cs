using System.Security.Claims;

namespace OrderSphere.Advisory.Api.Agent;

// Resolves the owning customer (Keycloak subject) from the current principal.
// JwtBearer maps "sub" to NameIdentifier by default; accept either.
internal static class CustomerContext
{
    public static string? ResolveSub(ClaimsPrincipal? user)
    {
        if (user?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var sub = user.FindFirst("sub")?.Value
                  ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return string.IsNullOrWhiteSpace(sub) ? null : sub;
    }
}
