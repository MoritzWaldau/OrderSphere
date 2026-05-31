using Microsoft.AspNetCore.Antiforgery;

namespace OrderSphere.Bff.Auth;

/// <summary>
/// Endpoint filter that validates the X-XSRF-TOKEN request header against the
/// antiforgery request token on all non-idempotent HTTP methods (POST, PUT, PATCH, DELETE).
/// Safe methods (GET, HEAD, OPTIONS, TRACE) pass through without validation.
/// Apply via .AddEndpointFilter&lt;AntiforgeryEndpointFilter&gt;() on individual BFF endpoints.
/// For the reverse-proxy route, antiforgery is validated by the dedicated middleware registered
/// in Program.cs before MapReverseProxy().
/// </summary>
public sealed class AntiforgeryEndpointFilter(IAntiforgery antiforgery) : IEndpointFilter
{
    private static readonly HashSet<string> SafeMethods =
        ["GET", "HEAD", "OPTIONS", "TRACE"];

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;

        if (!SafeMethods.Contains(httpContext.Request.Method.ToUpperInvariant()))
        {
            try
            {
                await antiforgery.ValidateRequestAsync(httpContext);
            }
            catch (AntiforgeryValidationException)
            {
                return Results.Problem(
                    title: "CSRF token validation failed.",
                    detail: "Include a valid X-XSRF-TOKEN header on all mutating requests.",
                    statusCode: StatusCodes.Status403Forbidden);
            }
        }

        return await next(context);
    }
}
