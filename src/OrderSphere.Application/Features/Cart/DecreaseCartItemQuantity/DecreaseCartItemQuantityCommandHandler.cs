using OrderSphere.Application.Abstraction;
using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Application.Features.Cart.DecreaseCartItemQuantity;

public sealed class DecreaseCartItemQuantityCommandHandler(IOrderingClient orderingClient)
    : ICommandHandler<DecreaseCartItemQuantityCommand, Result>
{
    public Task<Result> Handle(DecreaseCartItemQuantityCommand request, CancellationToken cancellationToken)
        => orderingClient.DecreaseCartItemQuantityAsync(request.CustomerId, request.ProductId, cancellationToken);
}
