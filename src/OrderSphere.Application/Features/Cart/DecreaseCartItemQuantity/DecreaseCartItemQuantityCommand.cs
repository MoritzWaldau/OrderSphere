using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Application.Features.Cart.DecreaseCartItemQuantity;

public sealed record DecreaseCartItemQuantityCommand(
    Guid CustomerId,
    Guid ProductId) : ICommand<Result>;

