using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using OrderSphere.UserProfile.Application.Features.Profile.UpdateProfile;
using OrderSphere.UserProfile.Domain.Entities;
using OrderSphere.UserProfile.Domain.Errors;
using OrderSphere.UserProfile.Tests.Helpers;
using Xunit;

namespace OrderSphere.UserProfile.Tests.Handlers;

public sealed class UpdateProfileCommandHandlerTests
{
    private static UpdateProfileCommandHandler CreateHandler(
        OrderSphere.UserProfile.Infrastructure.Persistence.UserProfileDbContext ctx)
        => new(ctx, NullLogger<UpdateProfileCommandHandler>.Instance);

    // ── Profile not found ─────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ProfileNotFound_ReturnsProfileNotFoundError()
    {
        await using var ctx = DbContextFactory.Create();
        var cmd = new UpdateProfileCommand("sub-missing", "X", "x@example.com");

        var result = await CreateHandler(ctx).Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserProfileErrors.ProfileNotFound);
    }

    // ── Happy path ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ProfileExists_UpdatesDisplayNameAndEmail()
    {
        await using var ctx = DbContextFactory.Create();

        var profile = new CustomerProfile("sub-upd", "Old Name", "old@example.com");
        ctx.CustomerProfiles.Add(profile);
        await ctx.SaveChangesAsync();

        var cmd = new UpdateProfileCommand("sub-upd", "New Name", "new@example.com");
        var result = await CreateHandler(ctx).Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DisplayName.Should().Be("New Name");
        result.Value.Email.Should().Be("new@example.com");
    }

    [Fact]
    public async Task Handle_ProfileExists_PersistsChangesToDatabase()
    {
        await using var ctx = DbContextFactory.Create();

        var profile = new CustomerProfile("sub-persist", "Before", "before@example.com");
        ctx.CustomerProfiles.Add(profile);
        await ctx.SaveChangesAsync();

        var cmd = new UpdateProfileCommand("sub-persist", "After", "after@example.com");
        await CreateHandler(ctx).Handle(cmd, CancellationToken.None);

        ctx.ChangeTracker.Clear();
        var stored = ctx.CustomerProfiles.Single(p => p.KeycloakSubject == "sub-persist");
        stored.DisplayName.Should().Be("After");
        stored.Email.Should().Be("after@example.com");
    }
}
