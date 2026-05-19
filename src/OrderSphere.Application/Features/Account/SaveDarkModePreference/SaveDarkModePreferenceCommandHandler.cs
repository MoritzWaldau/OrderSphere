using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using OrderSphere.Domain.Abstraction;
using OrderSphere.Domain.Entities;
using OrderSphere.Domain.Errors;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Application.Features.Account.SaveDarkModePreference;

public sealed class SaveDarkModePreferenceCommandHandler(
    UserManager<ApplicationUser> userManager,
    ILogger<SaveDarkModePreferenceCommandHandler> logger
) : ICommandHandler<SaveDarkModePreferenceCommand, Result>
{
    public async Task<Result> Handle(SaveDarkModePreferenceCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId);
        if (user is null)
        {
            logger.LogWarning("User {UserId} not found when saving dark mode preference", request.UserId);
            return Result.Failure(UserErrors.NotFound);
        }

        user.PrefersDarkMode = request.PrefersDarkMode;
        var result = await userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            logger.LogError("Failed to update dark mode preference for user {UserId}", request.UserId);
            return Result.Failure(UserErrors.UpdateFailed);
        }

        return Result.Success();
    }
}
