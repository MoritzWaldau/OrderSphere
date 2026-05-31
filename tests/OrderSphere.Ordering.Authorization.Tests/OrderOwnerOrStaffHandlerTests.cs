using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using OrderSphere.Ordering.Api.Authorization;
using Xunit;
using OrderSphere.Ordering.Application.Models;
using OrderSphere.Ordering.Domain.Enums;
using System.Security.Claims;

namespace OrderSphere.Ordering.Authorization.Tests;

/// <summary>
/// Unit tests for <see cref="OrderOwnerOrStaffHandler"/>.
///
/// The handler succeeds when:
///   (a) the caller's <c>sub</c> claim matches <c>OrderDto.CustomerId</c>, or
///   (b) the caller holds <c>csr</c>, <c>order-manager</c>, or <c>admin</c> role.
///
/// It does not explicitly fail the requirement; failing occurs when neither
/// condition is met and no other handler succeeds.
///
/// Tests drive the handler through the public <see cref="IAuthorizationHandler"/>
/// interface (<c>HandleAsync(context)</c>) rather than the protected
/// <c>HandleRequirementAsync</c> method.  The resource is supplied via
/// <see cref="AuthorizationHandlerContext"/> so the base-class dispatch works
/// correctly without any reflection.
/// </summary>
public sealed class OrderOwnerOrStaffHandlerTests
{
    private static readonly Guid OrderCustomerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherCustomerId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static readonly OrderDto SampleOrder = new(
        Id:             Guid.NewGuid(),
        CustomerId:     OrderCustomerId,
        Status:         OrderStatus.Created,
        PaymentMethod:  PaymentMethod.CreditCard,
        TrackingNumber: null,
        ShippingAddress: new OrderShippingAddressDto(
            "Jane", "Doe", "Teststr. 1", "Berlin", "10115", "DE"),
        Items: [],
        Total: 99.99m,
        CreatedAt: DateTime.UtcNow);

    private readonly IAuthorizationHandler _handler = new OrderOwnerOrStaffHandler();

    // ── Owner ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Owner_Succeeds()
    {
        var user    = BuildUser(sub: OrderCustomerId.ToString());
        var context = BuildContext(user, SampleOrder);

        await _handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue("the caller owns the order");
    }

    [Fact]
    public async Task DifferentCustomer_DoesNotSucceed()
    {
        var user    = BuildUser(sub: OtherCustomerId.ToString());
        var context = BuildContext(user, SampleOrder);

        await _handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse("the caller does not own the order");
    }

    [Fact]
    public async Task NullSub_DoesNotSucceed()
    {
        var user    = BuildUser(sub: null);
        var context = BuildContext(user, SampleOrder);

        await _handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task NonGuidSub_DoesNotSucceed()
    {
        var user    = BuildUser(sub: "not-a-guid");
        var context = BuildContext(user, SampleOrder);

        await _handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    // ── Staff roles ───────────────────────────────────────────────────────────

    [Theory]
    [InlineData("csr")]
    [InlineData("order-manager")]
    [InlineData("admin")]
    public async Task StaffRole_Succeeds(string role)
    {
        // Staff members can access any order regardless of ownership.
        var user    = BuildUser(sub: OtherCustomerId.ToString(), roles: [role]);
        var context = BuildContext(user, SampleOrder);

        await _handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue($"role '{role}' is a staff role");
    }

    [Fact]
    public async Task CustomerRoleOnly_DoesNotSucceedOnOtherOrder()
    {
        // A plain customer with no staff role cannot access another customer's order.
        var user    = BuildUser(sub: OtherCustomerId.ToString(), roles: ["customer"]);
        var context = BuildContext(user, SampleOrder);

        await _handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task AdminRole_SucceedsEvenWithDifferentSub()
    {
        var user    = BuildUser(sub: OtherCustomerId.ToString(), roles: ["admin"]);
        var context = BuildContext(user, SampleOrder);

        await _handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    // ── Owner + staff combination ─────────────────────────────────────────────

    [Fact]
    public async Task OwnerWithStaffRole_Succeeds()
    {
        var user    = BuildUser(sub: OrderCustomerId.ToString(), roles: ["csr"]);
        var context = BuildContext(user, SampleOrder);

        await _handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    // ── Unauthenticated ───────────────────────────────────────────────────────

    [Fact]
    public async Task UnauthenticatedUser_DoesNotSucceed()
    {
        var identity = new ClaimsIdentity();  // no authentication type → IsAuthenticated = false
        var user     = new ClaimsPrincipal(identity);
        var context  = BuildContext(user, SampleOrder);

        await _handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static ClaimsPrincipal BuildUser(string? sub, params string[] roles)
    {
        var claims = new List<Claim>();

        if (sub is not null)
            claims.Add(new Claim("sub", sub));

        foreach (var role in roles)
            claims.Add(new Claim("roles", role));

        // "authenticated" authenticationType makes IsAuthenticated = true.
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "authenticated", "sub", "roles"));
    }

    /// <summary>
    /// Builds an <see cref="AuthorizationHandlerContext"/> that contains the
    /// <see cref="OrderOwnerOrStaffRequirement"/> and the supplied resource.
    /// The resource must be in the context so the base-class
    /// <see cref="IAuthorizationHandler.HandleAsync"/> dispatch reaches
    /// <c>HandleRequirementAsync</c>.
    /// </summary>
    private static AuthorizationHandlerContext BuildContext(
        ClaimsPrincipal user,
        OrderDto        resource)
    {
        var requirement = new OrderOwnerOrStaffRequirement();
        return new AuthorizationHandlerContext([requirement], user, resource);
    }
}
