using MediatR;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Ordering.Api.Features.Cart;

public sealed record DecreaseCartItemQuantityCommand(Guid CustomerId, Guid ProductId)
    : IRequest<Result>;
