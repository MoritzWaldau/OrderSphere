using FluentAssertions;
using OrderSphere.UserProfile.Application.Features.Profile.OnboardingStatus;
using OrderSphere.UserProfile.Domain.Entities;
using OrderSphere.UserProfile.Domain.Errors;
using OrderSphere.UserProfile.Tests.Helpers;
using Xunit;

namespace OrderSphere.UserProfile.Tests.Handlers;

public sealed class GetOnboardingStatusQueryHandlerTests
{
    private static GetOnboardingStatusQueryHandler CreateHandler(
        OrderSphere.UserProfile.Infrastructure.Persistence.UserProfileDbContext ctx)
        => new(ctx);

    // ── Profile not found ─────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ProfileNotFound_ReturnsNotFoundError()
    {
        await using var ctx = DbContextFactory.Create();

        var result = await CreateHandler(ctx).Handle(
            new GetOnboardingStatusQuery("sub-missing"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserProfileErrors.ProfileNotFound);
    }

    // ── Status false ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_NewProfile_ReturnsFalse()
    {
        await using var ctx = DbContextFactory.Create();
        ctx.CustomerProfiles.Add(new CustomerProfile("sub-new", "Alice", "alice@example.com"));
        await ctx.SaveChangesAsync();

        var result = await CreateHandler(ctx).Handle(
            new GetOnboardingStatusQuery("sub-new"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    // ── Status true ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_CompletedProfile_ReturnsTrue()
    {
        await using var ctx = DbContextFactory.Create();
        var p = new CustomerProfile("sub-done", "Alice", "alice@example.com");
        p.MarkOnboardingComplete();
        ctx.CustomerProfiles.Add(p);
        await ctx.SaveChangesAsync();

        var result = await CreateHandler(ctx).Handle(
            new GetOnboardingStatusQuery("sub-done"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }
}
