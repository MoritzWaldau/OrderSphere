using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.BuildingBlocks.EventBus.AzureServiceBus.Dlq;

/// <summary>
/// Maps the admin-protected DLQ surface for a worker host. Each host calls this once with the route
/// prefix that the gateway forwards to it (e.g. <c>api/v1/admin/ordering/dlq</c>) and the name of an
/// authorization policy that requires the <c>admin</c> role.
/// </summary>
public static class DlqEndpointExtensions
{
    public static IEndpointRouteBuilder MapDlqAdminEndpoints(
        this IEndpointRouteBuilder endpoints,
        string routePrefix,
        string authorizationPolicy)
    {
        var group = endpoints.MapGroup(routePrefix)
            .RequireAuthorization(authorizationPolicy)
            .WithTags("DLQ Admin");

        group.MapGet("/", async (IDlqAdmin admin, CancellationToken ct) =>
                ToHttpResult(await admin.GetDepthsAsync(ct)))
            .WithName($"{routePrefix}/list")
            .WithSummary("Lists owned queues and their current dead-letter depth.");

        group.MapGet("/{queue}/messages", async (string queue, int? max, IDlqAdmin admin, CancellationToken ct) =>
                ToHttpResult(await admin.PeekAsync(queue, max ?? 20, ct)))
            .WithName($"{routePrefix}/peek")
            .WithSummary("Peeks dead-lettered messages on a queue without removing them.");

        group.MapPost("/{queue}/replay", async (string queue, ReplayRequest? body, IDlqAdmin admin, CancellationToken ct) =>
                ToHttpResult(await admin.ReplayAsync(queue, body?.Max ?? 20, ct)))
            .WithName($"{routePrefix}/replay")
            .WithSummary("Re-drives dead-lettered messages back onto the main queue.");

        return endpoints;
    }

    /// <summary>Body for a replay request. <see cref="Max"/> is clamped to the host's replay batch limit.</summary>
    public sealed record ReplayRequest(int Max);

    // Local Result → IResult mapping. Kept here rather than reusing ServiceDefaults.ToHttpResult so
    // this messaging assembly does not take a dependency on the hosting layer.
    private static IResult ToHttpResult<T>(Result<T> result) =>
        result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.Type switch
            {
                ErrorType.NotFound => Results.NotFound(new { result.Error.Code, result.Error.Description }),
                ErrorType.Forbidden => Results.Forbid(),
                ErrorType.Unauthorized => Results.Unauthorized(),
                _ => Results.BadRequest(new { result.Error.Code, result.Error.Description })
            };
}
