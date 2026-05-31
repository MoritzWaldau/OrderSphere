namespace OrderSphere.UserProfile.Application.Features.Profile.GetAddresses;

public sealed record GetAddressesQuery(string KeycloakSubject) : IQuery<Result<IReadOnlyList<AddressDto>>>;

public sealed class GetAddressesQueryHandler(IUserProfileDbContext context)
    : IQueryHandler<GetAddressesQuery, Result<IReadOnlyList<AddressDto>>>
{
    public async Task<Result<IReadOnlyList<AddressDto>>> Handle(GetAddressesQuery request, CancellationToken ct)
    {
        var profile = await context.CustomerProfiles
            .AsNoTracking()
            .Include(p => p.Addresses)
            .FirstOrDefaultAsync(p => p.KeycloakSubject == request.KeycloakSubject, ct);

        if (profile is null)
            return Result<IReadOnlyList<AddressDto>>.Failure(UserProfileErrors.ProfileNotFound);

        return Result<IReadOnlyList<AddressDto>>.Success(
            profile.Addresses.Select(ProfileMappers.ToAddressDto).ToList());
    }
}
