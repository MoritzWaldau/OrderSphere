using OrderSphere.Application.Abstraction;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Application.Features.Cart.RemoveFromCart;

public sealed class RemoveFromCartCommandHandler(IOrderingClient orderingClient)
    : ICommandHandler<RemoveFromCartCommand, Result>
{
    public Task<Result> Handle(RemoveFromCartCommand request, CancellationToken cancellationToken)
        => orderingClient.RemoveFromCartAsync(request.CustomerId, request.ProductId, cancellationToken);
}
