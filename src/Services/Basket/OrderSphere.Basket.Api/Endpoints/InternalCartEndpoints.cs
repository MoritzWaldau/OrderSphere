using MediatR;
using OrderSphere.Basket.Application.Features.Cart.ClearCart;
using OrderSphere.Basket.Application.Features.Cart.GetCartInternal;
using OrderSphere.BuildingBlocks.StronglyTypedIds;
using OrderSphere.ServiceDefaults;

namespace OrderSphere.Basket.Api.Endpoints;

public static class InternalCartEndpoints
{
    public static void MapInternalCartEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/internal/cart");

        group.MapGet("/{customerId:guid}", async (Guid customerId, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetCartInternalQuery(CustomerId.From(customerId)), ct);
            return result.ToHttpResult();
        });

        group.MapDelete("/{customerId:guid}/items", async (Guid customerId, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new ClearCartCommand(CustomerId.From(customerId)), ct);
            return result.ToHttpResult();
        });
    }
}
