namespace OrderSphere.UserProfile.Application.Features.Profile.CompleteOnboarding;

public sealed record CompleteOnboardingCommand(string Subject) : ICommand<Result<ProfileDto>>;

public sealed class CompleteOnboardingCommandValidator : AbstractValidator<CompleteOnboardingCommand>
{
    public CompleteOnboardingCommandValidator()
    {
        RuleFor(x => x.Subject).NotEmpty();
    }
}

public sealed class CompleteOnboardingCommandHandler(IUserProfileDbContext context)
    : ICommandHandler<CompleteOnboardingCommand, Result<ProfileDto>>
{
    public async Task<Result<ProfileDto>> Handle(CompleteOnboardingCommand request, CancellationToken ct)
    {
        var profile = await context.CustomerProfiles
            .Include(p => p.Addresses)
            .FirstOrDefaultAsync(p => p.Subject == request.Subject, ct);

        if (profile is null)
            return Result<ProfileDto>.Failure(UserProfileErrors.ProfileNotFound);

        if (string.IsNullOrWhiteSpace(profile.DisplayName))
            return Result<ProfileDto>.Failure(UserProfileErrors.OnboardingIncomplete);

        profile.MarkOnboardingComplete();
        await context.SaveChangesAsync(ct);

        return Result<ProfileDto>.Success(ProfileMappers.ToDto(profile));
    }
}
