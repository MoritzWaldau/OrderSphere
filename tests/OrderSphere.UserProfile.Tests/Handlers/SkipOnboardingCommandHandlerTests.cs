using FluentAssertions;
using OrderSphere.UserProfile.Application.Features.Profile.SkipOnboarding;
using OrderSphere.UserProfile.Domain.Entities;
using OrderSphere.UserProfile.Tests.Helpers;
using Xunit;

namespace OrderSphere.UserProfile.Tests.Handlers;

public sealed class SkipOnboardingCommandHandlerTests
{
    private static SkipOnboardingCommandHandler CreateHandler(
        OrderSphere.UserProfile.Infrastructure.Persistence.UserProfileDbContext ctx)
        => new(ctx);

    [Fact]
    public async Task Handle_ExistingProfile_SetsOnboardingCompleteWithoutAddress()
    {
        await using var ctx = DbContextFactory.Create();
        ctx.CustomerProfiles.Add(new CustomerProfile("sub-skip-existing", "Bob", "bob@example.com"));
        await ctx.SaveChangesAsync();

        var result = await CreateHandler(ctx).Handle(
            new SkipOnboardingCommand("sub-skip-existing", "Bob", "bob@example.com"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        ctx.CustomerProfiles.Single(p => p.Subject == "sub-skip-existing")
            .IsOnboardingComplete.Should().BeTrue();
        ctx.CustomerProfiles.Single(p => p.Subject == "sub-skip-existing")
            .Addresses.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_NoExistingProfile_CreatesProfileAndSetsOnboardingComplete()
    {
        await using var ctx = DbContextFactory.Create();

        var result = await CreateHandler(ctx).Handle(
            new SkipOnboardingCommand("sub-skip-new", "Carol", "carol@example.com"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var profile = ctx.CustomerProfiles.Single(p => p.Subject == "sub-skip-new");
        profile.IsOnboardingComplete.Should().BeTrue();
        profile.DisplayName.Should().Be("Carol");
        profile.Addresses.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_AlreadyComplete_SucceedsIdempotently()
    {
        await using var ctx = DbContextFactory.Create();
        var p = new CustomerProfile("sub-skip-idempotent", "Dave", "dave@example.com");
        p.MarkOnboardingComplete();
        ctx.CustomerProfiles.Add(p);
        await ctx.SaveChangesAsync();

        var result = await CreateHandler(ctx).Handle(
            new SkipOnboardingCommand("sub-skip-idempotent", "Dave", "dave@example.com"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsOnboardingComplete.Should().BeTrue();
    }
}
