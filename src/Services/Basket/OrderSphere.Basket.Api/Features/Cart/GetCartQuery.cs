using MediatR;
using OrderSphere.Basket.Api.Models;
using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Basket.Api.Features.Cart;

public sealed record GetCartQuery(Guid CustomerId) : IRequest<Result<CartDto>>;
