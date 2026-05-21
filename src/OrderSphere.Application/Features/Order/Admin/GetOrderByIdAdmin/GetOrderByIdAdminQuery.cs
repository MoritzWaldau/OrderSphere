using OrderSphere.Application.Models;
using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Application.Features.Order.Admin.GetOrderByIdAdmin;

/// <summary>
/// Admin-scoped query that does not enforce owner authorization.
/// Authorization is enforced at the route level via [Authorize(Roles = "Administrator")].
/// </summary>
public sealed record GetOrderByIdAdminQuery(Guid OrderId)
    : IQuery<Result<OrderDto>>;
