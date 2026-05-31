using Microsoft.Extensions.Logging;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.UserProfile.Application.Features.Profile;
using OrderSphere.UserProfile.Application.Models;
using OrderSphere.UserProfile.Domain.Errors;
using OrderSphere.UserProfile.Application.Abstractions;

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

public sealed class UpdateProfileCommandHandler(
    IUserProfileDbContext context,
    ILogger<UpdateProfileCommandHandler> logger
) : ICommandHandler<UpdateProfileCommand, Result<ProfileDto>>
{
    public async Task<Result<ProfileDto>> Handle(UpdateProfileCommand request, CancellationToken ct)
    {
        try
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
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating profile for subject {Subject}", request.KeycloakSubject);
            return Result<ProfileDto>.Failure(UserProfileErrors.UnknownError);
        }
    }
}
