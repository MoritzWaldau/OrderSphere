using FluentAssertions;
using OrderSphere.UserProfile.Application.Features.Profile.CompleteOnboarding;
using OrderSphere.UserProfile.Domain.Entities;
using OrderSphere.UserProfile.Domain.Errors;
using OrderSphere.UserProfile.Tests.Helpers;
using Xunit;

namespace OrderSphere.UserProfile.Tests.Handlers;

public sealed class CompleteOnboardingCommandHandlerTests
{
    private static CompleteOnboardingCommandHandler CreateHandler(
        OrderSphere.UserProfile.Infrastructure.Persistence.UserProfileDbContext ctx)
        => new(ctx);

    // ── Profile not found ─────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ProfileNotFound_ReturnsNotFoundError()
    {
        await using var ctx = DbContextFactory.Create();

        var result = await CreateHandler(ctx).Handle(
            new CompleteOnboardingCommand("sub-missing"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserProfileErrors.ProfileNotFound);
    }

    // ── Incomplete profile ────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_EmptyDisplayName_ReturnsOnboardingIncompleteError()
    {
        await using var ctx = DbContextFactory.Create();
        ctx.CustomerProfiles.Add(new CustomerProfile("sub-empty-name", string.Empty, "alice@example.com"));
        await ctx.SaveChangesAsync();

        var result = await CreateHandler(ctx).Handle(
            new CompleteOnboardingCommand("sub-empty-name"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserProfileErrors.OnboardingIncomplete);
    }

    // ── Happy path — address is now optional ─────────────────────────────────

    [Fact]
    public async Task Handle_ValidDisplayNameNoAddress_SetsIsOnboardingComplete()
    {
        await using var ctx = DbContextFactory.Create();
        ctx.CustomerProfiles.Add(new CustomerProfile("sub-no-addr", "Alice", "alice@example.com"));
        await ctx.SaveChangesAsync();

        var result = await CreateHandler(ctx).Handle(
            new CompleteOnboardingCommand("sub-no-addr"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        ctx.CustomerProfiles.Single(p => p.Subject == "sub-no-addr")
            .IsOnboardingComplete.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ValidProfile_ReturnsDtoWithFlagTrue()
    {
        await using var ctx = DbContextFactory.Create();
        ctx.CustomerProfiles.Add(new CustomerProfile("sub-dto", "Alice", "alice@example.com"));
        await ctx.SaveChangesAsync();

        var result = await CreateHandler(ctx).Handle(
            new CompleteOnboardingCommand("sub-dto"), CancellationToken.None);

        result.Value.IsOnboardingComplete.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_AlreadyComplete_SucceedsIdempotently()
    {
        await using var ctx = DbContextFactory.Create();
        var p = new CustomerProfile("sub-idempotent", "Alice", "alice@example.com");
        p.MarkOnboardingComplete();
        ctx.CustomerProfiles.Add(p);
        await ctx.SaveChangesAsync();

        var result = await CreateHandler(ctx).Handle(
            new CompleteOnboardingCommand("sub-idempotent"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsOnboardingComplete.Should().BeTrue();
    }
}
