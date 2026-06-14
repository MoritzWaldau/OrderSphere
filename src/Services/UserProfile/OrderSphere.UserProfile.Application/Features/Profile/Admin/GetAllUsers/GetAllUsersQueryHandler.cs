namespace OrderSphere.UserProfile.Application.Features.Profile.Admin.GetAllUsers;

public sealed record GetAllUsersQuery : IQuery<Result<IReadOnlyList<AdminUserSummaryDto>>>;

public sealed class GetAllUsersQueryHandler(IUserProfileDbContext context)
    : IQueryHandler<GetAllUsersQuery, Result<IReadOnlyList<AdminUserSummaryDto>>>
{
    public async Task<Result<IReadOnlyList<AdminUserSummaryDto>>> Handle(GetAllUsersQuery request, CancellationToken ct)
    {
        var users = await context.CustomerProfiles
            .AsNoTracking()
            .Include(p => p.Addresses)
            .OrderBy(p => p.DisplayName)
            .Select(p => new AdminUserSummaryDto(
                p.Id.Value,
                p.Subject,
                p.DisplayName,
                p.Email,
                p.DarkModeEnabled,
                p.Addresses.Count))
            .ToListAsync(ct);

        return Result<IReadOnlyList<AdminUserSummaryDto>>.Success(users);
    }
}
