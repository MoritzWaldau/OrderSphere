namespace OrderSphere.UserProfile.Application.Features.Profile.SkipOnboarding;

/// <summary>
/// Marks the onboarding prompt as dismissed without requiring any profile data.
/// Creates the profile record on first access if it doesn't exist yet.
/// IsOnboardingComplete = true means "prompt seen and handled" (filled or skipped).
/// </summary>
public sealed record SkipOnboardingCommand(
    string Subject,
    string DisplayName,
    string Email) : ICommand<Result<ProfileDto>>;

public sealed class SkipOnboardingCommandHandler(IUserProfileDbContext context)
    : ICommandHandler<SkipOnboardingCommand, Result<ProfileDto>>
{
    public async Task<Result<ProfileDto>> Handle(SkipOnboardingCommand request, CancellationToken ct)
    {
        var profile = await context.CustomerProfiles
            .Include(p => p.Addresses)
            .FirstOrDefaultAsync(p => p.Subject == request.Subject, ct);

        if (profile is null)
        {
            profile = new CustomerProfile(request.Subject, request.DisplayName, request.Email);
            context.CustomerProfiles.Add(profile);
        }

        profile.MarkOnboardingComplete();
        await context.SaveChangesAsync(ct);

        return Result<ProfileDto>.Success(ProfileMappers.ToDto(profile));
    }
}
