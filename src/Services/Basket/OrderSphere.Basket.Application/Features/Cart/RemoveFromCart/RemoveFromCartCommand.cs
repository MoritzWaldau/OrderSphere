using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.BuildingBlocks.StronglyTypedIds;

namespace OrderSphere.Basket.Application.Features.Cart.RemoveFromCart;

public sealed record RemoveFromCartCommand(CustomerId CustomerId, ProductId ProductId) : ICommand<Result>;
