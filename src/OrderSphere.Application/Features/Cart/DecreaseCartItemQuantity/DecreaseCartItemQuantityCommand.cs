using OrderSphere.Domain.Abstraction;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Application.Features.Cart.DecreaseCartItemQuantity;

public sealed record DecreaseCartItemQuantityCommand(
    Guid CustomerId,
    Guid ProductId) : ICommand<Result>;

