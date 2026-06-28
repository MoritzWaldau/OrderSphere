namespace OrderSphere.UserProfile.Application.Features.Profile.UpdateNotificationPreferences;

public sealed record UpdateNotificationPreferencesCommand(
    string Subject,
    bool EmailEnabled,
    bool SmsEnabled,
    bool PushEnabled) : ICommand<Result>;

public sealed class UpdateNotificationPreferencesCommandValidator : AbstractValidator<UpdateNotificationPreferencesCommand>
{
    public UpdateNotificationPreferencesCommandValidator()
    {
        RuleFor(x => x.Subject).NotEmpty();
    }
}

public sealed class UpdateNotificationPreferencesCommandHandler(IUserProfileDbContext context)
    : ICommandHandler<UpdateNotificationPreferencesCommand, Result>
{
    public async Task<Result> Handle(UpdateNotificationPreferencesCommand request, CancellationToken ct)
    {
        var profile = await context.CustomerProfiles
            .FirstOrDefaultAsync(p => p.Subject == request.Subject, ct);

        if (profile is null)
            return Result.Failure(UserProfileErrors.ProfileNotFound);

        profile.UpdateNotificationPreferences(
            request.EmailEnabled,
            request.SmsEnabled,
            request.PushEnabled,
            DateTime.UtcNow);

        await context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
