namespace OrderSphere.UserProfile.Application.Features.Profile.OnboardingStatus;

public sealed record GetOnboardingStatusQuery(string Subject) : IQuery<Result<bool>>;

public sealed class GetOnboardingStatusQueryHandler(IUserProfileDbContext context)
    : IQueryHandler<GetOnboardingStatusQuery, Result<bool>>
{
    public async Task<Result<bool>> Handle(GetOnboardingStatusQuery request, CancellationToken ct)
    {
        var status = await context.CustomerProfiles
            .AsNoTracking()
            .Where(p => p.Subject == request.Subject)
            .Select(p => (bool?)p.IsOnboardingComplete)
            .FirstOrDefaultAsync(ct);

        if (status is null)
            return Result<bool>.Failure(UserProfileErrors.ProfileNotFound);

        return Result<bool>.Success(status.Value);
    }
}
