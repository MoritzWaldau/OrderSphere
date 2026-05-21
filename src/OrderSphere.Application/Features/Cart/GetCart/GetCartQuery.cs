using OrderSphere.Application.Models;
using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Application.Features.Cart.GetCart;

public sealed record GetCartQuery(Guid CustomerId) : IQuery<Result<CartDto>>;
