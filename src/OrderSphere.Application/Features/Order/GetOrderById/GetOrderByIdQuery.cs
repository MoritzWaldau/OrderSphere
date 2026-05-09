using OrderSphere.Application.Models;
using OrderSphere.Domain.Abstraction;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Application.Features.Order.GetOrderById;

public sealed record GetOrderByIdQuery(Guid OrderId, Guid CustomerId)
    : IQuery<Result<OrderDto>>;
