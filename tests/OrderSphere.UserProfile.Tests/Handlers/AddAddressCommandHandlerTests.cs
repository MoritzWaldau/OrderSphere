using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrderSphere.UserProfile.Application.Features.Profile.AddAddress;
using OrderSphere.UserProfile.Domain.Entities;
using OrderSphere.UserProfile.Domain.Errors;
using OrderSphere.UserProfile.Tests.Helpers;
using Xunit;

namespace OrderSphere.UserProfile.Tests.Handlers;

public sealed class AddAddressCommandHandlerTests
{
    private static AddAddressCommandHandler CreateHandler(
        OrderSphere.UserProfile.Infrastructure.Persistence.UserProfileDbContext ctx)
        => new(ctx);

    private static AddAddressCommand ValidCommand(string sub, bool setAsDefault = false) => new(
        sub, "Home", "Alice", "Smith", "Hauptstr. 1", "Berlin", "10115", "DE", setAsDefault);


    [Fact]
    public async Task Handle_ProfileNotFound_ReturnsProfileNotFoundError()
    {
        await using var ctx = DbContextFactory.Create();

        var result = await CreateHandler(ctx).Handle(ValidCommand("sub-missing"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserProfileErrors.ProfileNotFound);
    }


    [Fact]
    public async Task Handle_TenAddressesAlready_ReturnsAddressLimitExceededError()
    {
        await using var ctx = DbContextFactory.Create();

        var profile = new CustomerProfile("sub-limit", "Dave", "dave@example.com");
        ctx.CustomerProfiles.Add(profile);
        await ctx.SaveChangesAsync();

        // Add 10 addresses directly via the aggregate to bypass the handler's limit check.
        for (var i = 0; i < 10; i++)
            profile.AddAddress($"Label{i}", "F", "L", "St", "City", "12345", "DE");
        await ctx.SaveChangesAsync();

        var result = await CreateHandler(ctx).Handle(ValidCommand("sub-limit"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserProfileErrors.AddressLimitExceeded);
    }


    [Fact]
    public async Task Handle_ValidCommand_AddsAddressAndReturnsDto()
    {
        await using var ctx = DbContextFactory.Create();

        var profile = new CustomerProfile("sub-add", "Eve", "eve@example.com");
        ctx.CustomerProfiles.Add(profile);
        await ctx.SaveChangesAsync();

        var result = await CreateHandler(ctx).Handle(ValidCommand("sub-add"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Label.Should().Be("Home");
        result.Value.City.Should().Be("Berlin");
        result.Value.Country.Should().Be("DE");
    }

    [Fact]
    public async Task Handle_FirstAddress_IsSetAsDefaultAutomatically()
    {
        await using var ctx = DbContextFactory.Create();

        var profile = new CustomerProfile("sub-first-addr", "Frank", "frank@example.com");
        ctx.CustomerProfiles.Add(profile);
        await ctx.SaveChangesAsync();

        var result = await CreateHandler(ctx).Handle(
            ValidCommand("sub-first-addr", setAsDefault: false), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsDefault.Should().BeTrue("the first address is always made default by the aggregate");
    }

    [Fact]
    public async Task Handle_AddressWithSetAsDefault_PromotesItAsDefault()
    {
        await using var ctx = DbContextFactory.Create();

        var profile = new CustomerProfile("sub-set-default", "Grace", "grace@example.com");
        ctx.CustomerProfiles.Add(profile);
        await ctx.SaveChangesAsync();

        // Seed one address that will start as default.
        await CreateHandler(ctx).Handle(ValidCommand("sub-set-default", setAsDefault: false), CancellationToken.None);

        // Add a second address explicitly requesting default.
        var cmd = new AddAddressCommand(
            "sub-set-default", "Work", "Grace", "Smith", "Allee 5", "Hamburg", "20099", "DE",
            SetAsDefault: true);
        var result = await CreateHandler(ctx).Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsDefault.Should().BeTrue();

        // Reload the profile and verify only one default exists.
        ctx.ChangeTracker.Clear();
        var stored = await ctx.CustomerProfiles
            .Include(p => p.Addresses)
            .SingleAsync(p => p.Subject == "sub-set-default");
        stored.Addresses.Count(a => a.IsDefault).Should().Be(1);
    }

    [Fact]
    public async Task Handle_ValidCommand_PersistsAddressToDatabaseDbContext()
    {
        await using var ctx = DbContextFactory.Create();

        var profile = new CustomerProfile("sub-persist-addr", "Hank", "hank@example.com");
        ctx.CustomerProfiles.Add(profile);
        await ctx.SaveChangesAsync();

        await CreateHandler(ctx).Handle(ValidCommand("sub-persist-addr"), CancellationToken.None);

        ctx.ChangeTracker.Clear();
        var stored = await ctx.CustomerProfiles
            .Include(p => p.Addresses)
            .SingleAsync(p => p.Subject == "sub-persist-addr");
        stored.Addresses.Should().HaveCount(1);
    }
}
