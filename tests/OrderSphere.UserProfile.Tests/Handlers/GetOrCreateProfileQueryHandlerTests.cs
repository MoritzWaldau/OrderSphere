using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using OrderSphere.UserProfile.Api.Features.Profile.GetOrCreateProfile;
using OrderSphere.UserProfile.Domain.Entities;
using OrderSphere.UserProfile.Tests.Helpers;
using Xunit;

namespace OrderSphere.UserProfile.Tests.Handlers;

public sealed class GetOrCreateProfileQueryHandlerTests
{
    private static GetOrCreateProfileQueryHandler CreateHandler(
        OrderSphere.UserProfile.Infrastructure.Persistence.UserProfileDbContext ctx)
        => new(ctx, NullLogger<GetOrCreateProfileQueryHandler>.Instance);

    // ── Profile does not exist ────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ProfileDoesNotExist_CreatesProfile()
    {
        await using var ctx = DbContextFactory.Create();
        var query = new GetOrCreateProfileQuery("sub-new", "Alice", "alice@example.com");

        var result = await CreateHandler(ctx).Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        ctx.CustomerProfiles.Should().ContainSingle(p => p.KeycloakSubject == "sub-new");
    }

    [Fact]
    public async Task Handle_ProfileDoesNotExist_ReturnsCorrectDto()
    {
        await using var ctx = DbContextFactory.Create();
        var query = new GetOrCreateProfileQuery("sub-new", "Alice", "alice@example.com");

        var result = await CreateHandler(ctx).Handle(query, CancellationToken.None);

        result.Value.KeycloakSubject.Should().Be("sub-new");
        result.Value.DisplayName.Should().Be("Alice");
        result.Value.Email.Should().Be("alice@example.com");
        result.Value.Addresses.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ProfileDoesNotExist_AuditFieldsPopulated()
    {
        await using var ctx = DbContextFactory.Create();
        var query = new GetOrCreateProfileQuery("sub-audit", "Bob", "bob@example.com");

        await CreateHandler(ctx).Handle(query, CancellationToken.None);

        var profile = ctx.CustomerProfiles.Single(p => p.KeycloakSubject == "sub-audit");
        profile.CreatedAt.Should().NotBe(default);
    }

    // ── Profile already exists ────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ProfileAlreadyExists_ReturnsExistingProfile()
    {
        await using var ctx = DbContextFactory.Create();

        var existing = new CustomerProfile("sub-existing", "Carol", "carol@example.com");
        ctx.CustomerProfiles.Add(existing);
        await ctx.SaveChangesAsync();

        var query = new GetOrCreateProfileQuery("sub-existing", "Updated Name", "updated@example.com");
        var result = await CreateHandler(ctx).Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DisplayName.Should().Be("Carol", "existing profile must not be overwritten");
        ctx.CustomerProfiles.Should().HaveCount(1, "no duplicate profile must be created");
    }
}
