using OrderSphere.Application.Abstraction;
using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Application.Features.Cart.AddToCart;

public sealed class AddToCartCommandHandler(IOrderingClient orderingClient)
    : ICommandHandler<AddToCartCommand, Result>
{
    public Task<Result> Handle(AddToCartCommand request, CancellationToken cancellationToken)
        => orderingClient.AddToCartAsync(request.CustomerId, request.ProductId, request.Quantity, cancellationToken);
}
