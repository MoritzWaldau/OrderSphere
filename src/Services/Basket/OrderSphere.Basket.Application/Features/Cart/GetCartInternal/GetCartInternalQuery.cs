using OrderSphere.Basket.Application.DTOs;
using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.BuildingBlocks.StronglyTypedIds;

namespace OrderSphere.Basket.Application.Features.Cart.GetCartInternal;

public sealed record GetCartInternalQuery(CustomerId CustomerId) : IQuery<Result<CartDto>>;
