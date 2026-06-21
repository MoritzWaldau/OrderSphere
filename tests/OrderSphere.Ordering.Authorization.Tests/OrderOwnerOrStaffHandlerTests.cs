using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using OrderSphere.BuildingBlocks.StronglyTypedIds;
using OrderSphere.Ordering.Api.Authorization;
using OrderSphere.Ordering.Application.Models;
using OrderSphere.Ordering.Domain.Enums;
using Xunit;

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
    // Auth0 sub values — opaque strings, not UUIDs.
    private const string OwnerSub = "auth0|owner-test-sub";
    private const string OtherSub = "auth0|other-test-sub";

    // CustomerId is derived from the sub via SHA256 (matching CustomerId.FromSub).
    private static readonly Guid OrderCustomerId = CustomerId.FromSub(OwnerSub).Value;
    private static readonly Guid OtherCustomerId = CustomerId.FromSub(OtherSub).Value;

    private static readonly OrderDto SampleOrder = new(
        Id: Guid.NewGuid(),
        CustomerId: OrderCustomerId,
        Status: OrderStatus.Created,
        PaymentMethod: PaymentMethod.CreditCard,
        TrackingNumber: null,
        ShippingAddress: new OrderShippingAddressDto(
            "Jane", "Doe", "Teststr. 1", "Berlin", "10115", "DE"),
        Items: [],
        Total: 99.99m,
        DiscountAmount: 0m,
        CouponCode: null,
        CreatedAt: DateTime.UtcNow);

    private readonly IAuthorizationHandler _handler = new OrderOwnerOrStaffHandler();


    [Fact]
    public async Task Owner_Succeeds()
    {
        var user = BuildUser(sub: OwnerSub);
        var context = BuildContext(user, SampleOrder);

        await _handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue("the caller owns the order");
    }

    [Fact]
    public async Task DifferentCustomer_DoesNotSucceed()
    {
        var user = BuildUser(sub: OtherSub);
        var context = BuildContext(user, SampleOrder);

        await _handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse("the caller does not own the order");
    }

    [Fact]
    public async Task NullSub_DoesNotSucceed()
    {
        var user = BuildUser(sub: null);
        var context = BuildContext(user, SampleOrder);

        await _handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task UnknownSub_DoesNotSucceed()
    {
        // Any sub that doesn't hash to OrderCustomerId must not succeed.
        var user = BuildUser(sub: "auth0|completely-unknown-sub");
        var context = BuildContext(user, SampleOrder);

        await _handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }


    [Theory]
    [InlineData("csr")]
    [InlineData("order-manager")]
    [InlineData("admin")]
    public async Task StaffRole_Succeeds(string role)
    {
        // Staff members can access any order regardless of ownership.
        var user = BuildUser(sub: OtherSub, roles: [role]);
        var context = BuildContext(user, SampleOrder);

        await _handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue($"role '{role}' is a staff role");
    }

    [Fact]
    public async Task CustomerRoleOnly_DoesNotSucceedOnOtherOrder()
    {
        // A plain customer with no staff role cannot access another customer's order.
        var user = BuildUser(sub: OtherSub, roles: ["customer"]);
        var context = BuildContext(user, SampleOrder);

        await _handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task AdminRole_SucceedsEvenWithDifferentSub()
    {
        var user = BuildUser(sub: OtherSub, roles: ["admin"]);
        var context = BuildContext(user, SampleOrder);

        await _handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }


    [Fact]
    public async Task OwnerWithStaffRole_Succeeds()
    {
        var user = BuildUser(sub: OwnerSub, roles: ["csr"]);
        var context = BuildContext(user, SampleOrder);

        await _handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }


    [Fact]
    public async Task UnauthenticatedUser_DoesNotSucceed()
    {
        var identity = new ClaimsIdentity();  // no authentication type → IsAuthenticated = false
        var user = new ClaimsPrincipal(identity);
        var context = BuildContext(user, SampleOrder);

        await _handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }


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
        OrderDto resource)
    {
        var requirement = new OrderOwnerOrStaffRequirement();
        return new AuthorizationHandlerContext([requirement], user, resource);
    }
}
