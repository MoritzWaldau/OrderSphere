using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrderSphere.BuildingBlocks.Contracts.Events;
using OrderSphere.BuildingBlocks.EventBus.Outbox;
using OrderSphere.UserProfile.Application.Features.Profile.RequestErasure;
using OrderSphere.UserProfile.Domain.Entities;
using OrderSphere.UserProfile.Domain.Errors;
using OrderSphere.UserProfile.Tests.Helpers;
using Xunit;

namespace OrderSphere.UserProfile.Tests.Handlers;

public sealed class RequestErasureCommandHandlerTests
{
    private static RequestErasureCommandHandler CreateHandler(
        OrderSphere.UserProfile.Infrastructure.Persistence.UserProfileDbContext ctx)
        => new(ctx);

    [Fact]
    public async Task Handle_ProfileNotFound_ReturnsProfileNotFoundError()
    {
        await using var ctx = DbContextFactory.Create();
        var cmd = new RequestErasureCommand("sub-missing");

        var result = await CreateHandler(ctx).Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserProfileErrors.ProfileNotFound);
    }

    [Fact]
    public async Task Handle_ProfileExists_AnonymizesProfileAndAddresses()
    {
        await using var ctx = DbContextFactory.Create();

        var profile = new CustomerProfile("sub-erase", "Jane", "jane@example.com");
        ctx.CustomerProfiles.Add(profile);
        await ctx.SaveChangesAsync();

        profile.AddAddress("Home", "Jane", "Doe", "Str 1", "Berlin", "10115", "DE");
        await ctx.SaveChangesAsync();

        var result = await CreateHandler(ctx).Handle(new RequestErasureCommand("sub-erase"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        ctx.ChangeTracker.Clear();
        var stored = await ctx.CustomerProfiles
            .IgnoreQueryFilters()
            .Include(p => p.Addresses)
            .SingleAsync(p => p.Subject == "sub-erase");

        stored.IsDeleted.Should().BeTrue();
        stored.Email.Should().NotBe("jane@example.com");
        stored.DisplayName.Should().NotBe("Jane");

        var address = stored.Addresses.Single();
        address.IsDeleted.Should().BeTrue();
        address.FirstName.Should().Be("Erased");
        address.Street.Should().Be("Erased");
    }

    [Fact]
    public async Task Handle_ProfileExists_StagesOutboxMessageForFanOut()
    {
        await using var ctx = DbContextFactory.Create();

        var profile = new CustomerProfile("sub-outbox", "Jane", "jane@example.com");
        ctx.CustomerProfiles.Add(profile);
        await ctx.SaveChangesAsync();

        await CreateHandler(ctx).Handle(new RequestErasureCommand("sub-outbox"), CancellationToken.None);

        var outboxMessage = await ctx.Set<OutboxMessage>().SingleAsync();
        outboxMessage.Type.Should().Be(nameof(CustomerErasureRequestedIntegrationEvent));
        outboxMessage.Content.Should().Contain("sub-outbox");
    }
}
