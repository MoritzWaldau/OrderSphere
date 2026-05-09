using OrderSphere.Application.Models;
using OrderSphere.Domain.Abstraction;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Application.Features.Order.GetOrderByCorrelationId;

public sealed record GetOrderByCorrelationIdQuery(Guid CorrelationId, Guid CustomerId)
    : IQuery<Result<OrderDto?>>;
