using OrderSphere.Application.Models;
using OrderSphere.Domain.Abstraction;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Application.Features.Cart.GetCart;

public sealed record GetCartQuery(Guid CustomerId) : IQuery<Result<CartDto>>;
