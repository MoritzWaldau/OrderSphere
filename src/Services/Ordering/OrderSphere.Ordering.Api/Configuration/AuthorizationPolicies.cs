namespace OrderSphere.Ordering.Api.Configuration;

/// <summary>
/// Central registry of ASP.NET authorization policy names for Ordering.Api.
/// </summary>
public static class AuthorizationPolicies
{
    /// <summary>Full platform administrators.</summary>
    public const string Admin = "Admin";

    /// <summary>
    /// Any staff role: <c>csr</c>, <c>order-manager</c>, or <c>admin</c>.
    /// Grants read-only access to all order data.
    /// </summary>
    public const string Staff = "Staff";

    /// <summary>
    /// Roles that may mutate order state: <c>order-manager</c> or <c>admin</c>.
    /// </summary>
    public const string OrderManager = "OrderManager";

    /// <summary>
    /// Resource-based policy: passes if the caller owns the order (Sub == Order.CustomerId)
    /// OR has a staff role (<c>csr</c>, <c>order-manager</c>, <c>admin</c>).
    /// Evaluated via <see cref="Microsoft.AspNetCore.Authorization.IAuthorizationService"/>.
    /// </summary>
    public const string OrderOwnerOrStaff = "OrderOwnerOrStaff";
}
