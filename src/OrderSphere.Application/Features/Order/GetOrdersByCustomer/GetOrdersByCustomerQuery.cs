using OrderSphere.Application.Models;
using OrderSphere.Domain.Abstraction;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Application.Features.Order.GetOrdersByCustomer;

public sealed record GetOrdersByCustomerQuery(Guid CustomerId)
    : IQuery<Result<IReadOnlyList<OrderDto>>>;
