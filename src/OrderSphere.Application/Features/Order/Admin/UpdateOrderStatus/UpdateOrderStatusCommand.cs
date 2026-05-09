using OrderSphere.Domain.Abstraction;
using OrderSphere.Domain.Enums;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Application.Features.Order.Admin.UpdateOrderStatus;

public sealed record UpdateOrderStatusCommand(Guid OrderId, OrderStatus NewStatus)
    : ICommand<Result<bool>>;
