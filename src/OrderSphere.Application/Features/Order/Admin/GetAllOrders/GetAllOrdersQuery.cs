using OrderSphere.Application.Models;
using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.Domain.Enums;

namespace OrderSphere.Application.Features.Order.Admin.GetAllOrders;

public sealed record GetAllOrdersQuery(OrderStatus? StatusFilter = null)
    : IQuery<Result<IReadOnlyList<OrderDto>>>;
