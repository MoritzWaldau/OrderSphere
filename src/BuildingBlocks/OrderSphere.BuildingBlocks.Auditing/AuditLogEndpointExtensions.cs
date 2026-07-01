using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.BuildingBlocks.Auditing;

public static class AuditLogEndpointExtensions
{
    public static IEndpointRouteBuilder MapAuditLogAdminEndpoints(
        this IEndpointRouteBuilder endpoints,
        string routePrefix,
        string authorizationPolicy)
    {
        var group = endpoints.MapGroup(routePrefix)
            .RequireAuthorization(authorizationPolicy)
            .WithTags("Audit Log Admin");

        group.MapGet("/", async (string? entityType, string? entityId, IAuditLogQuery query, CancellationToken ct) =>
                ToHttpResult(await query.QueryAsync(entityType, entityId, ct)))
            .WithName($"{routePrefix}/list")
            .WithSummary("Queries recorded audit-log entries, optionally filtered by entity type/id.");

        return endpoints;
    }

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
