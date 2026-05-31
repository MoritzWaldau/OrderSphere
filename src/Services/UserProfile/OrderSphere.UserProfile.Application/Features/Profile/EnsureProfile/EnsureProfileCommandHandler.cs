namespace OrderSphere.UserProfile.Application.Features.Profile.EnsureProfile;

/// <summary>
/// Returns the caller's profile, creating it on first access from the Keycloak
/// identity claims. Mutates state (insert on first call), hence a command.
/// </summary>
public sealed record EnsureProfileCommand(
    string KeycloakSubject,
    string DisplayName,
    string Email) : ICommand<Result<ProfileDto>>;

public sealed class EnsureProfileCommandHandler(IUserProfileDbContext context)
    : ICommandHandler<EnsureProfileCommand, Result<ProfileDto>>
{
    public async Task<Result<ProfileDto>> Handle(EnsureProfileCommand request, CancellationToken ct)
    {
        var profile = await context.CustomerProfiles
            .Include(p => p.Addresses)
            .FirstOrDefaultAsync(p => p.KeycloakSubject == request.KeycloakSubject, ct);

        if (profile is null)
        {
            profile = new CustomerProfile(request.KeycloakSubject, request.DisplayName, request.Email);
            context.CustomerProfiles.Add(profile);
            await context.SaveChangesAsync(ct);
        }

        return Result<ProfileDto>.Success(ProfileMappers.ToDto(profile));
    }
}
