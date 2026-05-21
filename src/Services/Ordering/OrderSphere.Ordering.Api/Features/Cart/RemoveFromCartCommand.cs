using MediatR;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Ordering.Api.Features.Cart;

public sealed record RemoveFromCartCommand(Guid CustomerId, Guid ProductId)
    : IRequest<Result>;
