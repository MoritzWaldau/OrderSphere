using MediatR;
using OrderSphere.Ordering.Api.Configuration;
using OrderSphere.Ordering.Application.Features.Saga;
using OrderSphere.ServiceDefaults;

namespace OrderSphere.Ordering.Api.Endpoints;

public static class SagaEndpoints
{
    public static void MapSagaEndpoints(this RouteGroupBuilder v1)
    {
        // Read-only saga observability for support/operations. Any staff role.
        var staffRead = v1.MapGroup("admin/sagas")
                          .RequireAuthorization(AuthorizationPolicies.Staff);

        staffRead.MapGet("/{correlationId:guid}",
            async (Guid correlationId, IMediator mediator, CancellationToken ct) =>
            {
                var result = await mediator.Send(new GetSagaByCorrelationIdQuery(correlationId), ct);
                if (result.IsFailure)
                    return result.ToHttpResult();

                return result.Value is null
                    ? Results.NotFound()
                    : Results.Ok(result.Value);
            });
    }
}
