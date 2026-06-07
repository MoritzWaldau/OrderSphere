using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;

namespace OrderSphere.Mcp.Server.Gateway;

// Forwards the caller's bearer token (the authenticated end-user, or an external
// MCP client's OAuth token) to the downstream API Gateway, so user-scoped
// endpoints (orders, profile) resolve data for the correct principal.
public sealed class BearerForwardingHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(token) && token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            request.Headers.Authorization = AuthenticationHeaderValue.Parse(token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
