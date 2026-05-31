using MediatR;
using Microsoft.AspNetCore.Authorization;
using OrderSphere.BuildingBlocks.Security;
using OrderSphere.Ordering.Api.Configuration;
using OrderSphere.Ordering.Application.Features.Order;
using OrderSphere.Ordering.Application.Features.Order.Admin;
using OrderSphere.Ordering.Application.Models;
using OrderSphere.Ordering.Domain.Enums;
using OrderSphere.ServiceDefaults;

namespace OrderSphere.Ordering.Api.Endpoints;

public static class OrderEndpoints
{
    public static void MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        // ── Customer endpoints ────────────────────────────────────────────────
        var customer = app.MapGroup("/api/v1/orders").RequireAuthorization();

        // GET /api/v1/orders — all orders for the authenticated customer.
        customer.MapGet("/",
            async (ICurrentUser currentUser, IMediator mediator, CancellationToken ct) =>
            {
                if (!TryGetCustomerId(currentUser, out var customerId))
                    return Results.Unauthorized();

                var result = await mediator.Send(new GetOrdersByCustomerQuery(customerId), ct);
                return result.ToHttpResult();
            });

        // GET /api/v1/orders/{orderId} — single order; ABAC: owner OR staff.
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

        // GET /api/v1/orders/correlation/{correlationId} — by Service Bus correlation ID; ABAC: owner OR staff.
        customer.MapGet("/correlation/{correlationId:guid}",
            async (Guid correlationId, IMediator mediator, IAuthorizationService authSvc,
                   HttpContext httpContext, CancellationToken ct) =>
            {
                var result = await mediator.Send(new GetOrderByCorrelationIdQuery(correlationId), ct);
                if (result.IsFailure)
                    return result.ToHttpResult();

                // Order may not be persisted yet (Service Bus processing latency) — return null body.
                if (result.Value is null)
                    return Results.Ok((OrderDto?)null);

                var authResult = await authSvc.AuthorizeAsync(
                    httpContext.User, result.Value, AuthorizationPolicies.OrderOwnerOrStaff);
                if (!authResult.Succeeded)
                    return Results.Forbid();

                return Results.Ok(result.Value);
            });

        // ── Admin / staff endpoints ───────────────────────────────────────────
        // Read operations: any staff role (csr, order-manager, admin).
        var staffRead = app.MapGroup("/api/v1/admin/orders")
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
        var orderManager = app.MapGroup("/api/v1/admin/orders")
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
        customerId = Guid.Empty;
        return currentUser.IsAuthenticated
            && currentUser.Sub is not null
            && Guid.TryParse(currentUser.Sub, out customerId);
    }
}

public sealed record UpdateStatusRequest(OrderStatus NewStatus);
