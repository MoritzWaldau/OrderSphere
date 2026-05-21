using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Application.Features.Order.Admin.CancelOrder;

public sealed record CancelOrderCommand(Guid OrderId)
    : ICommand<Result<bool>>;
