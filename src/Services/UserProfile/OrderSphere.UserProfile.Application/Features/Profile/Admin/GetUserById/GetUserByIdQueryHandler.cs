namespace OrderSphere.UserProfile.Application.Features.Profile.Admin.GetUserById;

public sealed record GetUserByIdQuery(Guid Id) : IQuery<Result<ProfileDto>>;

public sealed class GetUserByIdQueryHandler(IUserProfileDbContext context)
    : IQueryHandler<GetUserByIdQuery, Result<ProfileDto>>
{
    public async Task<Result<ProfileDto>> Handle(GetUserByIdQuery request, CancellationToken ct)
    {
        var profile = await context.CustomerProfiles
            .AsNoTracking()
            .Include(p => p.Addresses)
            .FirstOrDefaultAsync(p => p.Id == CustomerProfileId.From(request.Id), ct);

        if (profile is null)
            return Result<ProfileDto>.Failure(UserProfileErrors.ProfileNotFound);

        return Result<ProfileDto>.Success(ProfileMappers.ToDto(profile));
    }
}
