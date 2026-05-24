using MediatR;
using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Basket.Api.Features.Cart;

public sealed record DecreaseCartItemQuantityCommand(Guid CustomerId, Guid ProductId) : IRequest<Result>;
