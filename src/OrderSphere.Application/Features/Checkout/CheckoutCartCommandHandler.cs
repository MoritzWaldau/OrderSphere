using OrderSphere.Application.Abstraction;
using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Application.Features.Checkout;

public sealed class CheckoutCartCommandHandler(IOrderingClient orderingClient)
    : ICommandHandler<CheckoutCartCommand, Result<Guid>>
{
    public Task<Result<Guid>> Handle(CheckoutCartCommand request, CancellationToken cancellationToken)
        => orderingClient.CheckoutAsync(request.CheckoutCartDto, cancellationToken);
}
