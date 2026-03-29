using OrderSphere.Domain.Abstraction;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Application.Features.Cart.AddToCart;

public sealed record AddToCartCommand(Guid CustomerId, Guid ProductId, int Quantity) : ICommand<Result>;
