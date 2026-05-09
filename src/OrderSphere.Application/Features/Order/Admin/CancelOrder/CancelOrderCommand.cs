using OrderSphere.Domain.Abstraction;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Application.Features.Order.Admin.CancelOrder;

public sealed record CancelOrderCommand(Guid OrderId)
    : ICommand<Result<bool>>;
