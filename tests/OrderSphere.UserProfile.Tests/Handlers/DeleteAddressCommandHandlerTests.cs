using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using OrderSphere.UserProfile.Api.Features.Profile.DeleteAddress;
using OrderSphere.UserProfile.Domain.Entities;
using OrderSphere.UserProfile.Domain.Errors;
using OrderSphere.UserProfile.Tests.Helpers;
using Xunit;

namespace OrderSphere.UserProfile.Tests.Handlers;

public sealed class DeleteAddressCommandHandlerTests
{
    private static DeleteAddressCommandHandler CreateHandler(
        OrderSphere.UserProfile.Infrastructure.Persistence.UserProfileDbContext ctx)
        => new(ctx, NullLogger<DeleteAddressCommandHandler>.Instance);

    // ── Profile not found ─────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ProfileNotFound_ReturnsProfileNotFoundError()
    {
        await using var ctx = DbContextFactory.Create();
        var cmd = new DeleteAddressCommand("sub-missing", Guid.NewGuid());

        var result = await CreateHandler(ctx).Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserProfileErrors.ProfileNotFound);
    }

    // ── Address not found ─────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_AddressNotFound_ReturnsAddressNotFoundError()
    {
        await using var ctx = DbContextFactory.Create();

        var profile = new CustomerProfile("sub-no-addr", "Ivan", "ivan@example.com");
        ctx.CustomerProfiles.Add(profile);
        await ctx.SaveChangesAsync();

        var cmd = new DeleteAddressCommand("sub-no-addr", Guid.NewGuid());
        var result = await CreateHandler(ctx).Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserProfileErrors.AddressNotFound);
    }

    // ── Happy path ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_AddressExists_ReturnsSuccess()
    {
        await using var ctx = DbContextFactory.Create();

        var profile = new CustomerProfile("sub-del", "Jane", "jane@example.com");
        ctx.CustomerProfiles.Add(profile);
        await ctx.SaveChangesAsync();

        var address = profile.AddAddress("Home", "Jane", "Doe", "Str 1", "Berlin", "10115", "DE");
        await ctx.SaveChangesAsync();

        var cmd = new DeleteAddressCommand("sub-del", address.Id.Value);
        var result = await CreateHandler(ctx).Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_AddressExists_RemovesItFromDatabase()
    {
        await using var ctx = DbContextFactory.Create();

        var profile = new CustomerProfile("sub-del-persist", "Karl", "karl@example.com");
        ctx.CustomerProfiles.Add(profile);
        await ctx.SaveChangesAsync();

        var address = profile.AddAddress("Home", "Karl", "Doe", "Str 2", "Munich", "80331", "DE");
        await ctx.SaveChangesAsync();

        var cmd = new DeleteAddressCommand("sub-del-persist", address.Id.Value);
        await CreateHandler(ctx).Handle(cmd, CancellationToken.None);

        ctx.ChangeTracker.Clear();
        var stored = await ctx.CustomerProfiles
            .Include(p => p.Addresses)
            .SingleAsync(p => p.KeycloakSubject == "sub-del-persist");
        stored.Addresses.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_DeletingDefaultAddress_PromotesNextAddressAsDefault()
    {
        await using var ctx = DbContextFactory.Create();

        var profile = new CustomerProfile("sub-promote", "Leo", "leo@example.com");
        ctx.CustomerProfiles.Add(profile);
        await ctx.SaveChangesAsync();

        var first  = profile.AddAddress("First",  "L", "D", "St 1", "Berlin",  "10115", "DE");
        var second = profile.AddAddress("Second", "L", "D", "St 2", "Hamburg", "20095", "DE");
        await ctx.SaveChangesAsync();

        first.IsDefault.Should().BeTrue("first added address is always default");

        var cmd = new DeleteAddressCommand("sub-promote", first.Id.Value);
        await CreateHandler(ctx).Handle(cmd, CancellationToken.None);

        ctx.ChangeTracker.Clear();
        var stored = await ctx.CustomerProfiles
            .Include(p => p.Addresses)
            .SingleAsync(p => p.KeycloakSubject == "sub-promote");
        stored.Addresses.Should().ContainSingle().Which.IsDefault.Should().BeTrue();
        stored.Addresses.Single().Id.Should().Be(second.Id);
    }
}
