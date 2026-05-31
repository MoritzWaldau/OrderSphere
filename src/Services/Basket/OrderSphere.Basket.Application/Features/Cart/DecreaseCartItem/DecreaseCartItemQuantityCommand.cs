using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.BuildingBlocks.StronglyTypedIds;

namespace OrderSphere.Basket.Application.Features.Cart.DecreaseCartItem;

public sealed record DecreaseCartItemQuantityCommand(CustomerId CustomerId, ProductId ProductId) : ICommand<Result>;
