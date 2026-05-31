namespace OrderSphere.UserProfile.Application.Features.Profile.UpdatePreferences;

public sealed record UpdatePreferencesCommand(
    string KeycloakSubject,
    bool DarkModeEnabled) : ICommand<Result>;

public sealed class UpdatePreferencesCommandValidator : AbstractValidator<UpdatePreferencesCommand>
{
    public UpdatePreferencesCommandValidator()
    {
        RuleFor(x => x.KeycloakSubject).NotEmpty();
    }
}

public sealed class UpdatePreferencesCommandHandler(IUserProfileDbContext context)
    : ICommandHandler<UpdatePreferencesCommand, Result>
{
    public async Task<Result> Handle(UpdatePreferencesCommand request, CancellationToken ct)
    {
        var profile = await context.CustomerProfiles
            .FirstOrDefaultAsync(p => p.KeycloakSubject == request.KeycloakSubject, ct);

        if (profile is null)
            return Result.Failure(UserProfileErrors.ProfileNotFound);

        profile.SetDarkMode(request.DarkModeEnabled);
        await context.SaveChangesAsync(ct);

        return Result.Success();
    }
}
