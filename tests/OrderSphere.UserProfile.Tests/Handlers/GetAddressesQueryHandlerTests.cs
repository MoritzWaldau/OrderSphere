using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using OrderSphere.UserProfile.Api.Features.Profile.GetAddresses;
using OrderSphere.UserProfile.Domain.Entities;
using OrderSphere.UserProfile.Domain.Errors;
using OrderSphere.UserProfile.Tests.Helpers;
using Xunit;

namespace OrderSphere.UserProfile.Tests.Handlers;

public sealed class GetAddressesQueryHandlerTests
{
    private static GetAddressesQueryHandler CreateHandler(
        OrderSphere.UserProfile.Infrastructure.Persistence.UserProfileDbContext ctx)
        => new(ctx, NullLogger<GetAddressesQueryHandler>.Instance);

    // ── Profile not found ─────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ProfileNotFound_ReturnsProfileNotFoundError()
    {
        await using var ctx = DbContextFactory.Create();

        var result = await CreateHandler(ctx).Handle(
            new GetAddressesQuery("sub-missing"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserProfileErrors.ProfileNotFound);
    }

    // ── Profile with no addresses ─────────────────────────────────────────────

    [Fact]
    public async Task Handle_ProfileHasNoAddresses_ReturnsEmptyList()
    {
        await using var ctx = DbContextFactory.Create();
        ctx.CustomerProfiles.Add(new CustomerProfile("sub-empty", "Alice", "alice@example.com"));
        await ctx.SaveChangesAsync();

        var result = await CreateHandler(ctx).Handle(
            new GetAddressesQuery("sub-empty"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    // ── Happy path — addresses returned ──────────────────────────────────────

    [Fact]
    public async Task Handle_ProfileHasTwoAddresses_ReturnsBoth()
    {
        await using var ctx = DbContextFactory.Create();
        var profile = new CustomerProfile("sub-two-addr", "Bob", "bob@example.com");
        profile.AddAddress("Home", "Bob", "Smith", "Str. 1", "Berlin", "10115", "DE");
        profile.AddAddress("Work", "Bob", "Smith", "Office 2", "Munich", "80331", "DE");
        ctx.CustomerProfiles.Add(profile);
        await ctx.SaveChangesAsync();

        var result = await CreateHandler(ctx).Handle(
            new GetAddressesQuery("sub-two-addr"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }
}
