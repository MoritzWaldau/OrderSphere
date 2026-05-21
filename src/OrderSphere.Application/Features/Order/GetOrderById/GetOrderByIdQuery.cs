using OrderSphere.Application.Models;
using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Application.Features.Order.GetOrderById;

public sealed record GetOrderByIdQuery(Guid OrderId, Guid CustomerId)
    : IQuery<Result<OrderDto>>;
