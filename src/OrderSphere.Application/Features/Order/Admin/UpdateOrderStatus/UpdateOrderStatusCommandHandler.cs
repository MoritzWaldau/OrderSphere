using OrderSphere.Application.Abstraction;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Application.Features.Order.Admin.UpdateOrderStatus;

public sealed class UpdateOrderStatusCommandHandler(IOrderingClient orderingClient)
    : ICommandHandler<UpdateOrderStatusCommand, Result<bool>>
{
    public Task<Result<bool>> Handle(UpdateOrderStatusCommand request, CancellationToken cancellationToken)
        => orderingClient.UpdateOrderStatusAsync(request.OrderId, request.NewStatus, cancellationToken);
}
