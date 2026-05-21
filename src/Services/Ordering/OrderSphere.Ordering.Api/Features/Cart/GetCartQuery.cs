using MediatR;
using OrderSphere.Domain.Primitives;
using OrderSphere.Ordering.Api.Models;

namespace OrderSphere.Ordering.Api.Features.Cart;

public sealed record GetCartQuery(Guid CustomerId) : IRequest<Result<CartDto>>;
