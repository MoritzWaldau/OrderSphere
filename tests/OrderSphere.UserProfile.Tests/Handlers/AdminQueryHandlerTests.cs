using FluentAssertions;
using OrderSphere.UserProfile.Application.Features.Profile.Admin.GetAllUsers;
using OrderSphere.UserProfile.Application.Features.Profile.Admin.GetUserById;
using OrderSphere.UserProfile.Domain.Entities;
using OrderSphere.UserProfile.Domain.Errors;
using OrderSphere.UserProfile.Tests.Helpers;
using Xunit;

namespace OrderSphere.UserProfile.Tests.Handlers;

public sealed class AdminQueryHandlerTests
{

    [Fact]
    public async Task GetAllUsers_ReturnsProfilesOrderedByDisplayNameWithAddressCount()
    {
        await using var ctx = DbContextFactory.Create();

        var zoe = new CustomerProfile("sub-zoe", "Zoe", "zoe@example.com");
        zoe.AddAddress("Home", "Zoe", "Z", "St 1", "Berlin", "10115", "DE");
        var amy = new CustomerProfile("sub-amy", "Amy", "amy@example.com");
        ctx.CustomerProfiles.AddRange(zoe, amy);
        await ctx.SaveChangesAsync();

        var result = await new GetAllUsersQueryHandler(ctx)
            .Handle(new GetAllUsersQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value[0].DisplayName.Should().Be("Amy", "results are ordered by display name");
        result.Value.Single(u => u.Subject == "sub-zoe").AddressCount.Should().Be(1);
    }


    [Fact]
    public async Task GetUserById_ProfileExists_ReturnsProfileDto()
    {
        await using var ctx = DbContextFactory.Create();

        var profile = new CustomerProfile("sub-byid", "Mona", "mona@example.com");
        ctx.CustomerProfiles.Add(profile);
        await ctx.SaveChangesAsync();

        var result = await new GetUserByIdQueryHandler(ctx)
            .Handle(new GetUserByIdQuery(profile.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Subject.Should().Be("sub-byid");
    }

    [Fact]
    public async Task GetUserById_ProfileMissing_ReturnsProfileNotFound()
    {
        await using var ctx = DbContextFactory.Create();

        var result = await new GetUserByIdQueryHandler(ctx)
            .Handle(new GetUserByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserProfileErrors.ProfileNotFound);
    }
}
