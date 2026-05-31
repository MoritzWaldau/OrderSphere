using Microsoft.AspNetCore.Authorization;

namespace OrderSphere.Ordering.Api.Authorization;

/// <summary>
/// Authorization requirement satisfied when the caller either owns the
/// order (token <c>sub</c> matches <c>Order.CustomerId</c>) or holds a
/// staff role (<c>csr</c>, <c>order-manager</c>, or <c>admin</c>).
/// </summary>
public sealed class OrderOwnerOrStaffRequirement : IAuthorizationRequirement { }
