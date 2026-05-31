using Microsoft.AspNetCore.Authorization;
using OrderSphere.Ordering.Application.Models;
using System.Security.Claims;

namespace OrderSphere.Ordering.Api.Authorization;

/// <summary>
/// Handles <see cref="OrderOwnerOrStaffRequirement"/> against an <see cref="OrderDto"/> resource.
/// Succeeds when:
/// <list type="bullet">
///   <item>The caller's <c>sub</c> claim parses as the same <see cref="Guid"/> as
///     <see cref="OrderDto.CustomerId"/> (owner), or</item>
///   <item>The caller holds the <c>csr</c>, <c>order-manager</c>, or <c>admin</c> role.</item>
/// </list>
/// Does not fail the requirement — callers without a matching sub and no staff role
/// simply do not succeed, causing the framework to deny access.
/// </summary>
public sealed class OrderOwnerOrStaffHandler
    : AuthorizationHandler<OrderOwnerOrStaffRequirement, OrderDto>
{
    private static readonly string[] StaffRoles = ["csr", "order-manager", "admin"];

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OrderOwnerOrStaffRequirement requirement,
        OrderDto resource)
    {
        // Staff bypass: any of the defined roles suffices.
        if (StaffRoles.Any(context.User.IsInRole))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Owner check: token sub must parse and match the order's CustomerId.
        var sub = context.User.FindFirstValue("sub");
        if (sub is not null
            && Guid.TryParse(sub, out var subGuid)
            && subGuid == resource.CustomerId)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
