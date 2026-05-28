using OrderSphere.Basket.Api.Models;
using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.BuildingBlocks.StronglyTypedIds;

namespace OrderSphere.Basket.Api.Features.Cart;

public sealed record GetCartQuery(CustomerId CustomerId) : IQuery<Result<CartDto>>;
