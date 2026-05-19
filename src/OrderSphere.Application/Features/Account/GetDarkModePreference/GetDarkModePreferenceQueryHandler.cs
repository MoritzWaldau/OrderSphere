using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using OrderSphere.Domain.Abstraction;
using OrderSphere.Domain.Entities;
using OrderSphere.Domain.Errors;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Application.Features.Account.GetDarkModePreference;

public sealed class GetDarkModePreferenceQueryHandler(
    UserManager<ApplicationUser> userManager,
    ILogger<GetDarkModePreferenceQueryHandler> logger
) : IQueryHandler<GetDarkModePreferenceQuery, Result<bool>>
{
    public async Task<Result<bool>> Handle(GetDarkModePreferenceQuery request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId);
        if (user is null)
        {
            logger.LogWarning("User {UserId} not found when reading dark mode preference", request.UserId);
            return Result<bool>.Failure(UserErrors.NotFound);
        }

        return Result<bool>.Success(user.PrefersDarkMode);
    }
}
