using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.BuildingBlocks.StronglyTypedIds;

namespace OrderSphere.Basket.Api.Features.Cart;

public sealed record RemoveFromCartCommand(CustomerId CustomerId, ProductId ProductId) : ICommand<Result>;
