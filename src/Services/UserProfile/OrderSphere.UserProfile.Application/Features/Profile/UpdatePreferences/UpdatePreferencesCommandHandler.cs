namespace OrderSphere.UserProfile.Application.Features.Profile.UpdatePreferences;

public sealed record UpdatePreferencesCommand(
    string Subject,
    bool DarkModeEnabled) : ICommand<Result>;

public sealed class UpdatePreferencesCommandValidator : AbstractValidator<UpdatePreferencesCommand>
{
    public UpdatePreferencesCommandValidator()
    {
        RuleFor(x => x.Subject).NotEmpty();
    }
}

public sealed class UpdatePreferencesCommandHandler(IUserProfileDbContext context)
    : ICommandHandler<UpdatePreferencesCommand, Result>
{
    public async Task<Result> Handle(UpdatePreferencesCommand request, CancellationToken ct)
    {
        var profile = await context.CustomerProfiles
            .FirstOrDefaultAsync(p => p.Subject == request.Subject, ct);

        if (profile is null)
            return Result.Failure(UserProfileErrors.ProfileNotFound);

        profile.SetDarkMode(request.DarkModeEnabled);
        await context.SaveChangesAsync(ct);

        return Result.Success();
    }
}
