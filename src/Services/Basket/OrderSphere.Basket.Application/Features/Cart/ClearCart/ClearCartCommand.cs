using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.BuildingBlocks.StronglyTypedIds;

namespace OrderSphere.Basket.Application.Features.Cart.ClearCart;

public sealed record ClearCartCommand(CustomerId CustomerId) : ICommand<Result>;
