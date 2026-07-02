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
        // D4 — requires a valid client-credentials token (any authenticated caller); M2M
        // tokens carry no role claims, so no role-based policy is applied here.
        var group = app.MapGroup("/internal/cart").RequireAuthorization();

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
