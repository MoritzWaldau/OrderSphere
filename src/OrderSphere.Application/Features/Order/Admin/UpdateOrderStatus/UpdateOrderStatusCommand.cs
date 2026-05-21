using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.Domain.Enums;

namespace OrderSphere.Application.Features.Order.Admin.UpdateOrderStatus;

public sealed record UpdateOrderStatusCommand(Guid OrderId, OrderStatus NewStatus)
    : ICommand<Result<bool>>;
