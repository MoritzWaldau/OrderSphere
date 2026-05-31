namespace OrderSphere.UserProfile.Application.Features.Profile.UpdateProfile;

public sealed record UpdateProfileCommand(
    string KeycloakSubject,
    string DisplayName,
    string Email) : ICommand<Result<ProfileDto>>;

public sealed class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(x => x.KeycloakSubject).NotEmpty();
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
    }
}

public sealed class UpdateProfileCommandHandler(IUserProfileDbContext context)
    : ICommandHandler<UpdateProfileCommand, Result<ProfileDto>>
{
    public async Task<Result<ProfileDto>> Handle(UpdateProfileCommand request, CancellationToken ct)
    {
        var profile = await context.CustomerProfiles
            .Include(p => p.Addresses)
            .FirstOrDefaultAsync(p => p.KeycloakSubject == request.KeycloakSubject, ct);

        if (profile is null)
            return Result<ProfileDto>.Failure(UserProfileErrors.ProfileNotFound);

        profile.UpdateDetails(request.DisplayName, request.Email);
        await context.SaveChangesAsync(ct);

        return Result<ProfileDto>.Success(ProfileMappers.ToDto(profile));
    }
}
