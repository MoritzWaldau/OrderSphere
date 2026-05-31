using Microsoft.Extensions.Logging;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.UserProfile.Application.Features.Profile;
using OrderSphere.UserProfile.Application.Models;
using OrderSphere.UserProfile.Domain.Entities;
using OrderSphere.UserProfile.Domain.Errors;
using OrderSphere.UserProfile.Application.Abstractions;

namespace OrderSphere.UserProfile.Application.Features.Profile.GetOrCreateProfile;

public sealed record GetOrCreateProfileQuery(
    string KeycloakSubject,
    string DisplayName,
    string Email) : IQuery<Result<ProfileDto>>;

public sealed class GetOrCreateProfileQueryHandler(
    IUserProfileDbContext context,
    ILogger<GetOrCreateProfileQueryHandler> logger
) : IQueryHandler<GetOrCreateProfileQuery, Result<ProfileDto>>
{
    public async Task<Result<ProfileDto>> Handle(GetOrCreateProfileQuery request, CancellationToken ct)
    {
        try
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
        catch (Exception ex)
        {
            logger.LogError(ex, "Error loading/creating profile for subject {Subject}", request.KeycloakSubject);
            return Result<ProfileDto>.Failure(UserProfileErrors.UnknownError);
        }
    }
}
