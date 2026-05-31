using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using OrderSphere.UserProfile.Application.Features.Profile.SetDefaultAddress;
using OrderSphere.UserProfile.Domain.Entities;
using OrderSphere.UserProfile.Domain.Errors;
using OrderSphere.UserProfile.Tests.Helpers;
using Xunit;

namespace OrderSphere.UserProfile.Tests.Handlers;

public sealed class SetDefaultAddressCommandHandlerTests
{
    private static SetDefaultAddressCommandHandler CreateHandler(
        OrderSphere.UserProfile.Infrastructure.Persistence.UserProfileDbContext ctx)
        => new(ctx, NullLogger<SetDefaultAddressCommandHandler>.Instance);

    // ── Profile not found ─────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ProfileNotFound_ReturnsProfileNotFoundError()
    {
        await using var ctx = DbContextFactory.Create();
        var cmd = new SetDefaultAddressCommand("sub-missing", Guid.NewGuid());

        var result = await CreateHandler(ctx).Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserProfileErrors.ProfileNotFound);
    }

    // ── Address not found ─────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_AddressNotFound_ReturnsAddressNotFoundError()
    {
        await using var ctx = DbContextFactory.Create();

        var profile = new CustomerProfile("sub-no-addr", "Mia", "mia@example.com");
        ctx.CustomerProfiles.Add(profile);
        await ctx.SaveChangesAsync();

        var cmd = new SetDefaultAddressCommand("sub-no-addr", Guid.NewGuid());
        var result = await CreateHandler(ctx).Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserProfileErrors.AddressNotFound);
    }

    // ── Happy path ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidAddressId_ReturnsSuccess()
    {
        await using var ctx = DbContextFactory.Create();

        var profile = new CustomerProfile("sub-setdef", "Nick", "nick@example.com");
        ctx.CustomerProfiles.Add(profile);
        await ctx.SaveChangesAsync();

        var addr = profile.AddAddress("Home", "N", "D", "St 1", "Berlin", "10115", "DE");
        await ctx.SaveChangesAsync();

        var cmd = new SetDefaultAddressCommand("sub-setdef", addr.Id.Value);
        var result = await CreateHandler(ctx).Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_SecondAddressTargeted_SwitchesDefaultCorrectly()
    {
        await using var ctx = DbContextFactory.Create();

        var profile = new CustomerProfile("sub-switch", "Olivia", "olivia@example.com");
        ctx.CustomerProfiles.Add(profile);
        await ctx.SaveChangesAsync();

        var first  = profile.AddAddress("First",  "O", "D", "St 1", "Berlin",  "10115", "DE");
        var second = profile.AddAddress("Second", "O", "D", "St 2", "Hamburg", "20095", "DE");
        await ctx.SaveChangesAsync();

        first.IsDefault.Should().BeTrue("first address is default initially");

        var cmd = new SetDefaultAddressCommand("sub-switch", second.Id.Value);
        await CreateHandler(ctx).Handle(cmd, CancellationToken.None);

        ctx.ChangeTracker.Clear();
        var stored = await ctx.CustomerProfiles
            .Include(p => p.Addresses)
            .SingleAsync(p => p.KeycloakSubject == "sub-switch");

        stored.Addresses.Single(a => a.Id == second.Id).IsDefault.Should().BeTrue();
        stored.Addresses.Single(a => a.Id == first.Id).IsDefault.Should().BeFalse();
        stored.Addresses.Count(a => a.IsDefault).Should().Be(1, "exactly one address must be default");
    }
}
