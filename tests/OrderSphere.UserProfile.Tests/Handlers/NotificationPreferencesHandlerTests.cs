using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrderSphere.UserProfile.Application.Features.Profile.GetNotificationPreferences;
using OrderSphere.UserProfile.Application.Features.Profile.UpdateNotificationPreferences;
using OrderSphere.UserProfile.Domain.Entities;
using OrderSphere.UserProfile.Domain.Errors;
using OrderSphere.UserProfile.Tests.Helpers;
using Xunit;

namespace OrderSphere.UserProfile.Tests.Handlers;

public sealed class NotificationPreferencesHandlerTests
{
    // --- GetNotificationPreferences ---

    [Fact]
    public async Task Get_ProfileNotFound_ReturnsError()
    {
        await using var ctx = DbContextFactory.Create();

        var result = await new GetNotificationPreferencesQueryHandler(ctx)
            .Handle(new GetNotificationPreferencesQuery("sub-missing"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserProfileErrors.ProfileNotFound);
    }

    [Fact]
    public async Task Get_NewProfile_ReturnsDefaults_EmailOnly()
    {
        await using var ctx = DbContextFactory.Create();
        ctx.CustomerProfiles.Add(new CustomerProfile("sub-new", "Alice", "alice@example.com"));
        await ctx.SaveChangesAsync();

        var result = await new GetNotificationPreferencesQueryHandler(ctx)
            .Handle(new GetNotificationPreferencesQuery("sub-new"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.EmailEnabled.Should().BeTrue();
        result.Value.SmsEnabled.Should().BeFalse();
        result.Value.PushEnabled.Should().BeFalse();
        result.Value.ConsentedAt.Should().BeNull();
    }


    // --- UpdateNotificationPreferences ---

    [Fact]
    public async Task Update_ProfileNotFound_ReturnsError()
    {
        await using var ctx = DbContextFactory.Create();

        var result = await new UpdateNotificationPreferencesCommandHandler(ctx)
            .Handle(new UpdateNotificationPreferencesCommand("sub-missing", true, false, false), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserProfileErrors.ProfileNotFound);
    }

    [Fact]
    public async Task Update_AllChannelsEnabled_PersistsCorrectly()
    {
        await using var ctx = DbContextFactory.Create();
        ctx.CustomerProfiles.Add(new CustomerProfile("sub-bob", "Bob", "bob@example.com"));
        await ctx.SaveChangesAsync();

        var result = await new UpdateNotificationPreferencesCommandHandler(ctx)
            .Handle(new UpdateNotificationPreferencesCommand("sub-bob", true, true, true), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        ctx.ChangeTracker.Clear();
        var stored = await ctx.CustomerProfiles.FirstAsync(p => p.Subject == "sub-bob");
        stored.NotificationPreferences.EmailEnabled.Should().BeTrue();
        stored.NotificationPreferences.SmsEnabled.Should().BeTrue();
        stored.NotificationPreferences.PushEnabled.Should().BeTrue();
        stored.NotificationPreferences.ConsentedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Update_OptOutSms_KeepsEmailEnabled()
    {
        await using var ctx = DbContextFactory.Create();
        var profile = new CustomerProfile("sub-carol", "Carol", "carol@example.com");
        profile.UpdateNotificationPreferences(true, true, false, DateTime.UtcNow);
        ctx.CustomerProfiles.Add(profile);
        await ctx.SaveChangesAsync();

        var result = await new UpdateNotificationPreferencesCommandHandler(ctx)
            .Handle(new UpdateNotificationPreferencesCommand("sub-carol", true, false, false), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        ctx.ChangeTracker.Clear();
        var stored = await ctx.CustomerProfiles.FirstAsync(p => p.Subject == "sub-carol");
        stored.NotificationPreferences.EmailEnabled.Should().BeTrue();
        stored.NotificationPreferences.SmsEnabled.Should().BeFalse();
        stored.NotificationPreferences.PushEnabled.Should().BeFalse();
    }
}
