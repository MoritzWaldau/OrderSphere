using MediatR;
using OrderSphere.Ordering.Api.Features.Order;
using OrderSphere.Ordering.Api.Features.Order.Admin;
using OrderSphere.Ordering.Api.Models;
using OrderSphere.Ordering.Domain.Enums;

namespace OrderSphere.Ordering.Api.Endpoints;

public static class OrderEndpoints
{
    public static void MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        // Customer endpoints
        var customer = app.MapGroup("/api/v1/orders").RequireAuthorization();

        customer.MapGet("/customer/{customerId:guid}",
            async (Guid customerId, IMediator mediator, CancellationToken ct) =>
            {
                var result = await mediator.Send(new GetOrdersByCustomerQuery(customerId), ct);
                return result.IsSuccess ? Results.Ok(result.Value)
                    : Results.BadRequest(new ErrorResponse(result.Error.Code, result.Error.Description));
            });

        customer.MapGet("/{orderId:guid}/customer/{customerId:guid}",
            async (Guid orderId, Guid customerId, IMediator mediator, CancellationToken ct) =>
            {
                var result = await mediator.Send(new GetOrderByIdQuery(orderId, customerId), ct);
                return result.IsSuccess ? Results.Ok(result.Value)
                    : Results.NotFound(new ErrorResponse(result.Error.Code, result.Error.Description));
            });

        customer.MapGet("/correlation/{correlationId:guid}/customer/{customerId:guid}",
            async (Guid correlationId, Guid customerId, IMediator mediator, CancellationToken ct) =>
            {
                var result = await mediator.Send(new GetOrderByCorrelationIdQuery(correlationId, customerId), ct);
                return result.IsSuccess ? Results.Ok(result.Value) // may be null → 200 with null body
                    : Results.BadRequest(new ErrorResponse(result.Error.Code, result.Error.Description));
            });

        // Admin endpoints
        var admin = app.MapGroup("/api/v1/admin/orders").RequireAuthorization("AdminPolicy");

        admin.MapGet("/",
            async (OrderStatus? status, IMediator mediator, CancellationToken ct) =>
            {
                var result = await mediator.Send(new GetAllOrdersQuery(status), ct);
                return result.IsSuccess ? Results.Ok(result.Value)
                    : Results.BadRequest(new ErrorResponse(result.Error.Code, result.Error.Description));
            });

        admin.MapGet("/stats",
            async (IMediator mediator, CancellationToken ct) =>
            {
                var result = await mediator.Send(new GetOrderStatsQuery(), ct);
                return result.IsSuccess ? Results.Ok(result.Value)
                    : Results.BadRequest(new ErrorResponse(result.Error.Code, result.Error.Description));
            });

        admin.MapGet("/{orderId:guid}",
            async (Guid orderId, IMediator mediator, CancellationToken ct) =>
            {
                var result = await mediator.Send(new GetOrderByIdAdminQuery(orderId), ct);
                return result.IsSuccess ? Results.Ok(result.Value)
                    : Results.NotFound(new ErrorResponse(result.Error.Code, result.Error.Description));
            });

        admin.MapPut("/{orderId:guid}/status",
            async (Guid orderId, UpdateStatusRequest req, IMediator mediator, CancellationToken ct) =>
            {
                var result = await mediator.Send(new UpdateOrderStatusCommand(orderId, req.NewStatus), ct);
                return result.IsSuccess ? Results.Ok()
                    : Results.BadRequest(new ErrorResponse(result.Error.Code, result.Error.Description));
            });

        admin.MapPost("/{orderId:guid}/cancel",
            async (Guid orderId, IMediator mediator, CancellationToken ct) =>
            {
                var result = await mediator.Send(new CancelOrderCommand(orderId), ct);
                return result.IsSuccess ? Results.Ok()
                    : Results.BadRequest(new ErrorResponse(result.Error.Code, result.Error.Description));
            });
    }
}

public sealed record UpdateStatusRequest(OrderStatus NewStatus);
