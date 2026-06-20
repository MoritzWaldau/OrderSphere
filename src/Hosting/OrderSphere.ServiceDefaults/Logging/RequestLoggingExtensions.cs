using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting;

public static class RequestLoggingExtensions
{
    private static readonly string[] ExcludedPrefixes =
    [
        "/health",
        "/alive",
        "/version",
    ];

    /// <summary>
    /// Registers ASP.NET Core HTTP logging (method, path, status, duration) and the
    /// request-context enrichment middleware (user_id, client IP in log scope).
    /// Call before builder.Build().
    /// </summary>
    public static IServiceCollection AddOrderSphereRequestLogging(this IServiceCollection services)
    {
        services.AddHttpLogging(options =>
        {
            // Metadata only — no request/response bodies or header values to avoid PII.
            options.LoggingFields = HttpLoggingFields.RequestMethod
                | HttpLoggingFields.RequestPath
                | HttpLoggingFields.ResponseStatusCode
                | HttpLoggingFields.Duration;

            // One combined log entry per request instead of separate request/response entries.
            options.CombineLogs = true;
        });

        return services;
    }

    /// <summary>
    /// Activates HTTP request logging and context enrichment.
    /// Insert after UseAuthentication/UseAuthorization so that user_id is available in scope.
    /// Health/liveness/version endpoints are excluded to suppress noise.
    /// </summary>
    public static WebApplication UseOrderSphereRequestLogging(this WebApplication app)
    {
        app.UseWhen(
            ctx => !IsExcluded(ctx.Request.Path),
            branch =>
            {
                branch.UseHttpLogging();
                branch.UseMiddleware<RequestContextEnrichmentMiddleware>();
            });

        return app;
    }

    private static bool IsExcluded(PathString path)
    {
        foreach (var prefix in ExcludedPrefixes)
        {
            if (path.StartsWithSegments(prefix))
                return true;
        }
        return false;
    }
}
