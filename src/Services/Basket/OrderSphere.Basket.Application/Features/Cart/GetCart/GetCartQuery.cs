using OrderSphere.Basket.Application.DTOs;
using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.BuildingBlocks.StronglyTypedIds;

namespace OrderSphere.Basket.Application.Features.Cart.GetCart;

public sealed record GetCartQuery(CustomerId CustomerId) : IQuery<Result<CartDto>>;
