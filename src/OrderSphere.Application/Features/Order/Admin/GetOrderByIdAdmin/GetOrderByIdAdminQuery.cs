using OrderSphere.Application.Models;
using OrderSphere.Domain.Abstraction;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Application.Features.Order.Admin.GetOrderByIdAdmin;

/// <summary>
/// Admin-scoped query that does not enforce owner authorization.
/// Authorization is enforced at the route level via [Authorize(Roles = "Administrator")].
/// </summary>
public sealed record GetOrderByIdAdminQuery(Guid OrderId)
    : IQuery<Result<OrderDto>>;
