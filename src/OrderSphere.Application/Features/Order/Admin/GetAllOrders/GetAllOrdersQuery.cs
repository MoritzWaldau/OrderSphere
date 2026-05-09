using OrderSphere.Application.Models;
using OrderSphere.Domain.Abstraction;
using OrderSphere.Domain.Enums;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Application.Features.Order.Admin.GetAllOrders;

public sealed record GetAllOrdersQuery(OrderStatus? StatusFilter = null)
    : IQuery<Result<IReadOnlyList<OrderDto>>>;
