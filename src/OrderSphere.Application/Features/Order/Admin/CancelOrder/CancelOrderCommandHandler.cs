using OrderSphere.Application.Abstraction;
using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Application.Features.Order.Admin.CancelOrder;

public sealed class CancelOrderCommandHandler(IOrderingClient orderingClient)
    : ICommandHandler<CancelOrderCommand, Result<bool>>
{
    public Task<Result<bool>> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
        => orderingClient.CancelOrderAsync(request.OrderId, cancellationToken);
}
