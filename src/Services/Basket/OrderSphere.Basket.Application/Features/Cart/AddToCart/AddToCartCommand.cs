using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.BuildingBlocks.StronglyTypedIds;

namespace OrderSphere.Basket.Application.Features.Cart.AddToCart;

public sealed record AddToCartCommand(CustomerId CustomerId, ProductId ProductId, int Quantity) : ICommand<Result>;
