using MediatR;
using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.BuildingBlocks.StronglyTypedIds;

namespace OrderSphere.Basket.Api.Features.Cart;

public sealed record DecreaseCartItemQuantityCommand(CustomerId CustomerId, ProductId ProductId) : IRequest<Result>;
