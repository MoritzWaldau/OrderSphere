using OrderSphere.Application.Models;
using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Application.Features.Order.GetOrdersByCustomer;

public sealed record GetOrdersByCustomerQuery(Guid CustomerId)
    : IQuery<Result<IReadOnlyList<OrderDto>>>;
