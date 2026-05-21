using MediatR;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Ordering.Api.Features.Cart;

public sealed record AddToCartCommand(Guid CustomerId, Guid ProductId, int Quantity)
    : IRequest<Result>;
