using MediatR;
using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Ordering.Api.Features.Cart;

public sealed record AddToCartCommand(Guid CustomerId, Guid ProductId, int Quantity)
    : IRequest<Result>;
