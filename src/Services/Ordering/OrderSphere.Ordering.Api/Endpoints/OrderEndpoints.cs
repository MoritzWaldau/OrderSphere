using MediatR;
using Microsoft.AspNetCore.Authorization;
using OrderSphere.BuildingBlocks.Security;
using OrderSphere.BuildingBlocks.StronglyTypedIds;
using OrderSphere.Ordering.Api.Configuration;
using OrderSphere.Ordering.Application.Features.Order;
using OrderSphere.Ordering.Application.Features.Order.Admin;
using OrderSphere.Ordering.Domain.Enums;
using OrderSphere.ServiceDefaults;

namespace OrderSphere.Ordering.Api.Endpoints;

public static class OrderEndpoints
{
    public static void MapOrderEndpoints(this RouteGroupBuilder v1)
    {
        // ── Customer endpoints ────────────────────────────────────────────────
        var customer = v1.MapGroup("orders").RequireAuthorization();

        customer.MapGet("/",
            async (ICurrentUser currentUser, IMediator mediator, CancellationToken ct) =>
            {
                if (!TryGetCustomerId(currentUser, out var customerId))
                    return Results.Unauthorized();

                var result = await mediator.Send(new GetOrdersByCustomerQuery(customerId), ct);
                return result.ToHttpResult();
            });

        // GET orders/{orderId} — single order; ABAC: owner OR staff.
        customer.MapGet("/{orderId:guid}",
            async (Guid orderId, IMediator mediator, IAuthorizationService authSvc,
                   HttpContext httpContext, CancellationToken ct) =>
            {
                var result = await mediator.Send(new GetOrderByIdQuery(orderId), ct);
                if (result.IsFailure)
                    return result.ToHttpResult();

                var authResult = await authSvc.AuthorizeAsync(
                    httpContext.User, result.Value, AuthorizationPolicies.OrderOwnerOrStaff);
                if (!authResult.Succeeded)
                    return Results.Forbid();

                return Results.Ok(result.Value);
            });

        // GET orders/correlation/{correlationId} — by Service Bus correlation ID; ABAC: owner OR staff.
        customer.MapGet("/correlation/{correlationId:guid}",
            async (Guid correlationId, IMediator mediator, IAuthorizationService authSvc,
                   HttpContext httpContext, CancellationToken ct) =>
            {
                var result = await mediator.Send(new GetOrderByCorrelationIdQuery(correlationId), ct);
                if (result.IsFailure)
                    return result.ToHttpResult();

                // Order may not be persisted yet (Service Bus processing latency).
                // Signal "not ready" with 204 — an empty 200 body breaks JSON deserialization on the client.
                if (result.Value is null)
                    return Results.NoContent();

                var authResult = await authSvc.AuthorizeAsync(
                    httpContext.User, result.Value, AuthorizationPolicies.OrderOwnerOrStaff);
                if (!authResult.Succeeded)
                    return Results.Forbid();

                return Results.Ok(result.Value);
            });

        // ── Admin / staff endpoints ───────────────────────────────────────────
        // Read operations: any staff role (csr, order-manager, admin).
        var staffRead = v1.MapGroup("admin/orders")
                           .RequireAuthorization(AuthorizationPolicies.Staff);

        staffRead.MapGet("/",
            async (OrderStatus? status, IMediator mediator, CancellationToken ct) =>
            {
                var result = await mediator.Send(new GetAllOrdersQuery(status), ct);
                return result.ToHttpResult();
            });

        staffRead.MapGet("/stats",
            async (IMediator mediator, CancellationToken ct) =>
            {
                var result = await mediator.Send(new GetOrderStatsQuery(), ct);
                return result.ToHttpResult();
            });

        staffRead.MapGet("/{orderId:guid}",
            async (Guid orderId, IMediator mediator, CancellationToken ct) =>
            {
                var result = await mediator.Send(new GetOrderByIdAdminQuery(orderId), ct);
                return result.ToHttpResult();
            });

        // Write operations: order-manager or admin.
        var orderManager = v1.MapGroup("admin/orders")
                              .RequireAuthorization(AuthorizationPolicies.OrderManager);

        orderManager.MapPut("/{orderId:guid}/status",
            async (Guid orderId, UpdateStatusRequest req, IMediator mediator, CancellationToken ct) =>
            {
                var result = await mediator.Send(new UpdateOrderStatusCommand(orderId, req.NewStatus), ct);
                return result.ToHttpResult(() => Results.Ok());
            });

        orderManager.MapPost("/{orderId:guid}/cancel",
            async (Guid orderId, IMediator mediator, CancellationToken ct) =>
            {
                var result = await mediator.Send(new CancelOrderCommand(orderId), ct);
                return result.ToHttpResult(() => Results.Ok());
            });
    }

    private static bool TryGetCustomerId(ICurrentUser currentUser, out Guid customerId)
    {
        if (!currentUser.IsAuthenticated || currentUser.Sub is null)
        {
            customerId = Guid.Empty;
            return false;
        }
        customerId = CustomerId.FromSub(currentUser.Sub).Value;
        return true;
    }
}

public sealed record UpdateStatusRequest(OrderStatus NewStatus);
