using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using OrderSphere.UserProfile.Api.Features.Profile.UpdatePreferences;
using OrderSphere.UserProfile.Domain.Entities;
using OrderSphere.UserProfile.Domain.Errors;
using OrderSphere.UserProfile.Tests.Helpers;
using Xunit;

namespace OrderSphere.UserProfile.Tests.Handlers;

public sealed class UpdatePreferencesCommandHandlerTests
{
    private static UpdatePreferencesCommandHandler CreateHandler(
        OrderSphere.UserProfile.Infrastructure.Persistence.UserProfileDbContext ctx)
        => new(ctx, NullLogger<UpdatePreferencesCommandHandler>.Instance);

    // ── Profile not found ─────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ProfileNotFound_ReturnsProfileNotFoundError()
    {
        await using var ctx = DbContextFactory.Create();

        var result = await CreateHandler(ctx).Handle(
            new UpdatePreferencesCommand("sub-missing", DarkModeEnabled: true),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserProfileErrors.ProfileNotFound);
    }

    // ── DarkMode enabled ──────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_DarkModeEnabled_PersistsTrueValue()
    {
        await using var ctx = DbContextFactory.Create();
        ctx.CustomerProfiles.Add(new CustomerProfile("sub-dark", "Carol", "carol@example.com"));
        await ctx.SaveChangesAsync();

        var result = await CreateHandler(ctx).Handle(
            new UpdatePreferencesCommand("sub-dark", DarkModeEnabled: true),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        ctx.ChangeTracker.Clear();
        var stored = await ctx.CustomerProfiles.FirstAsync(p => p.KeycloakSubject == "sub-dark");
        stored.DarkModeEnabled.Should().BeTrue();
    }

    // ── DarkMode disabled ─────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_DarkModeDisabled_PersistsFalseValue()
    {
        await using var ctx = DbContextFactory.Create();
        var profile = new CustomerProfile("sub-light", "Dave", "dave@example.com");
        profile.SetDarkMode(true);
        ctx.CustomerProfiles.Add(profile);
        await ctx.SaveChangesAsync();

        var result = await CreateHandler(ctx).Handle(
            new UpdatePreferencesCommand("sub-light", DarkModeEnabled: false),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        ctx.ChangeTracker.Clear();
        var stored = await ctx.CustomerProfiles.FirstAsync(p => p.KeycloakSubject == "sub-light");
        stored.DarkModeEnabled.Should().BeFalse();
    }
}
