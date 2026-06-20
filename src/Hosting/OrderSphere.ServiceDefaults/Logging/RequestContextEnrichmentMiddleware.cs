using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Pushes request-scoped identifiers (user_id, client_ip) into the ILogger scope
/// so every log entry written during that request carries these fields as structured
/// properties — visible as Custom Dimensions in Application Insights.
/// </summary>
internal sealed class RequestContextEnrichmentMiddleware(RequestDelegate next, ILogger<RequestContextEnrichmentMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var userId = context.User.FindFirstValue("sub")
            ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? "-";

        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "-";

        // X-Forwarded-For takes precedence when running behind a reverse proxy / gateway.
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwarded)
            && !string.IsNullOrWhiteSpace(forwarded))
        {
            clientIp = forwarded.ToString().Split(',')[0].Trim();
        }

        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["user_id"] = userId,
            ["client_ip"] = clientIp,
        }))
        {
            await next(context);
        }
    }
}
