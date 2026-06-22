using MediatR;
using OrderSphere.Ordering.Api.Configuration;
using OrderSphere.Ordering.Application.Features.OrderHistory;
using OrderSphere.Ordering.Application.Features.OrderHistory.Admin;
using OrderSphere.ServiceDefaults;

namespace OrderSphere.Ordering.Api.Endpoints;

/// <summary>
/// Read-only observability over the <c>order_history</c> CQRS read-model. Staff-scoped, like
/// saga observability — a customer-facing per-order timeline is already served from the order
/// write aggregate (<c>OrderDto.StatusHistory</c>).
/// </summary>
public static class OrderHistoryEndpoints
{
    public static void MapOrderHistoryEndpoints(this RouteGroupBuilder v1)
    {
        var staffRead = v1.MapGroup("admin/orders")
                          .RequireAuthorization(AuthorizationPolicies.Staff);

        // Paged, cross-order activity feed (newest first); optional customerEmail filter.
        staffRead.MapGet("/history",
            async (int? page, int? pageSize, string? customerEmail, IMediator mediator, CancellationToken ct) =>
            {
                var result = await mediator.Send(
                    new GetOrderHistoryFeedQuery(page ?? 1, pageSize ?? 20, customerEmail), ct);
                return result.ToHttpResult();
            });

        // Per-order status timeline (oldest first).
        staffRead.MapGet("/{orderId:guid}/history",
            async (Guid orderId, IMediator mediator, CancellationToken ct) =>
            {
                var result = await mediator.Send(new GetOrderHistoryForOrderQuery(orderId), ct);
                return result.ToHttpResult();
            });
    }
}
