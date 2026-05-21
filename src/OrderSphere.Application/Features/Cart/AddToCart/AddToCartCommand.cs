using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Application.Features.Cart.AddToCart;

public sealed record AddToCartCommand(Guid CustomerId, Guid ProductId, int Quantity) : ICommand<Result>;
