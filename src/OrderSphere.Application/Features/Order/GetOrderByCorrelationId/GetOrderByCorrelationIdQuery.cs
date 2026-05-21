using OrderSphere.Application.Models;
using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Application.Features.Order.GetOrderByCorrelationId;

public sealed record GetOrderByCorrelationIdQuery(Guid CorrelationId, Guid CustomerId)
    : IQuery<Result<OrderDto?>>;
