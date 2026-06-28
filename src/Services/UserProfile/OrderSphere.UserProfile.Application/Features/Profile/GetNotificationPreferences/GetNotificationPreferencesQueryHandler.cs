namespace OrderSphere.UserProfile.Application.Features.Profile.GetNotificationPreferences;

public sealed record GetNotificationPreferencesQuery(string Subject) : IQuery<Result<NotificationPreferencesDto>>;

public sealed class GetNotificationPreferencesQueryHandler(IUserProfileDbContext context)
    : IQueryHandler<GetNotificationPreferencesQuery, Result<NotificationPreferencesDto>>
{
    public async Task<Result<NotificationPreferencesDto>> Handle(
        GetNotificationPreferencesQuery request,
        CancellationToken ct)
    {
        var profile = await context.CustomerProfiles
            .FirstOrDefaultAsync(p => p.Subject == request.Subject, ct);

        if (profile is null)
            return Result<NotificationPreferencesDto>.Failure(UserProfileErrors.ProfileNotFound);

        var prefs = profile.NotificationPreferences;
        return Result<NotificationPreferencesDto>.Success(
            new NotificationPreferencesDto(prefs.EmailEnabled, prefs.SmsEnabled, prefs.PushEnabled, prefs.ConsentedAt));
    }
}
