using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.UserProfile.Domain.Errors;
using OrderSphere.UserProfile.Infrastructure.Persistence;

namespace OrderSphere.UserProfile.Api.Features.Profile.UpdatePreferences;

public sealed record UpdatePreferencesCommand(
    string KeycloakSubject,
    bool DarkModeEnabled) : ICommand<Result>;

public sealed class UpdatePreferencesCommandValidator : AbstractValidator<UpdatePreferencesCommand>
{
    public UpdatePreferencesCommandValidator()
    {
        RuleFor(x => x.KeycloakSubject).NotEmpty();
    }
}

public sealed class UpdatePreferencesCommandHandler(
    UserProfileDbContext context,
    ILogger<UpdatePreferencesCommandHandler> logger
) : ICommandHandler<UpdatePreferencesCommand, Result>
{
    public async Task<Result> Handle(UpdatePreferencesCommand request, CancellationToken ct)
    {
        try
        {
            var profile = await context.CustomerProfiles
                .FirstOrDefaultAsync(p => p.KeycloakSubject == request.KeycloakSubject, ct);

            if (profile is null)
                return Result.Failure(UserProfileErrors.ProfileNotFound);

            profile.SetDarkMode(request.DarkModeEnabled);
            await context.SaveChangesAsync(ct);

            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating preferences for subject {Subject}", request.KeycloakSubject);
            return Result.Failure(UserProfileErrors.UnknownError);
        }
    }
}
