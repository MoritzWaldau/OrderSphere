using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.BuildingBlocks.StronglyTypedIds;

namespace OrderSphere.Basket.Api.Features.Cart;

public sealed record AddToCartCommand(CustomerId CustomerId, ProductId ProductId, int Quantity) : ICommand<Result>;
