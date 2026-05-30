using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using OrderSphere.BuildingBlocks.StronglyTypedIds;
using OrderSphere.UserProfile.Api.Features.Profile.UpdateAddress;
using OrderSphere.UserProfile.Domain.Entities;
using OrderSphere.UserProfile.Domain.Errors;
using OrderSphere.UserProfile.Tests.Helpers;
using Xunit;

namespace OrderSphere.UserProfile.Tests.Handlers;

public sealed class UpdateAddressCommandHandlerTests
{
    private static UpdateAddressCommandHandler CreateHandler(
        OrderSphere.UserProfile.Infrastructure.Persistence.UserProfileDbContext ctx)
        => new(ctx, NullLogger<UpdateAddressCommandHandler>.Instance);

    private static UpdateAddressCommand ValidCommand(string sub, Guid addressId) => new(
        sub, addressId, "Work", "Alice", "Smith",
        "New Street 5", "Hamburg", "20099", "DE");

    // ── Profile not found ─────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ProfileNotFound_ReturnsProfileNotFoundError()
    {
        await using var ctx = DbContextFactory.Create();

        var result = await CreateHandler(ctx).Handle(
            ValidCommand("sub-missing", Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserProfileErrors.ProfileNotFound);
    }

    // ── Address not found ─────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_AddressNotFound_ReturnsAddressNotFoundError()
    {
        await using var ctx = DbContextFactory.Create();
        ctx.CustomerProfiles.Add(new CustomerProfile("sub-no-addr", "Eve", "eve@example.com"));
        await ctx.SaveChangesAsync();

        var result = await CreateHandler(ctx).Handle(
            ValidCommand("sub-no-addr", Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserProfileErrors.AddressNotFound);
    }

    // ── Happy path — address updated ──────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidCommand_UpdatesAddressAndReturnsDto()
    {
        await using var ctx = DbContextFactory.Create();
        var profile = new CustomerProfile("sub-update-addr", "Frank", "frank@example.com");
        profile.AddAddress("Home", "Frank", "Smith", "Old Street 1", "Berlin", "10115", "DE");
        ctx.CustomerProfiles.Add(profile);
        await ctx.SaveChangesAsync();

        // Retrieve the created address id
        ctx.ChangeTracker.Clear();
        var stored = await ctx.CustomerProfiles
            .Include(p => p.Addresses)
            .SingleAsync(p => p.KeycloakSubject == "sub-update-addr");
        var addressId = stored.Addresses.Single().Id.Value;

        var result = await CreateHandler(ctx).Handle(
            ValidCommand("sub-update-addr", addressId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.City.Should().Be("Hamburg");
        result.Value.Street.Should().Be("New Street 5");
        result.Value.Label.Should().Be("Work");
    }

    // ── Persistence verified ──────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidCommand_PersistsChangesToDatabase()
    {
        await using var ctx = DbContextFactory.Create();
        var profile = new CustomerProfile("sub-persist-upd", "Grace", "grace@example.com");
        profile.AddAddress("Old", "Grace", "Lee", "Old St 1", "Frankfurt", "60311", "DE");
        ctx.CustomerProfiles.Add(profile);
        await ctx.SaveChangesAsync();

        ctx.ChangeTracker.Clear();
        var stored = await ctx.CustomerProfiles
            .Include(p => p.Addresses)
            .SingleAsync(p => p.KeycloakSubject == "sub-persist-upd");
        var addressId = stored.Addresses.Single().Id.Value;

        await CreateHandler(ctx).Handle(ValidCommand("sub-persist-upd", addressId), CancellationToken.None);

        ctx.ChangeTracker.Clear();
        var refreshed = await ctx.CustomerProfiles
            .Include(p => p.Addresses)
            .SingleAsync(p => p.KeycloakSubject == "sub-persist-upd");
        refreshed.Addresses.Single().City.Should().Be("Hamburg");
    }
}
