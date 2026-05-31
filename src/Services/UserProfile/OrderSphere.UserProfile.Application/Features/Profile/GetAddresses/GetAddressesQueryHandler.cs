using Microsoft.Extensions.Logging;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.UserProfile.Application.Features.Profile;
using OrderSphere.UserProfile.Application.Models;
using OrderSphere.UserProfile.Domain.Errors;
using OrderSphere.UserProfile.Application.Abstractions;

namespace OrderSphere.UserProfile.Application.Features.Profile.GetAddresses;

public sealed record GetAddressesQuery(string KeycloakSubject) : IQuery<Result<IReadOnlyList<AddressDto>>>;

public sealed class GetAddressesQueryHandler(
    IUserProfileDbContext context,
    ILogger<GetAddressesQueryHandler> logger
) : IQueryHandler<GetAddressesQuery, Result<IReadOnlyList<AddressDto>>>
{
    public async Task<Result<IReadOnlyList<AddressDto>>> Handle(GetAddressesQuery request, CancellationToken ct)
    {
        try
        {
            var profile = await context.CustomerProfiles
                .Include(p => p.Addresses)
                .FirstOrDefaultAsync(p => p.KeycloakSubject == request.KeycloakSubject, ct);

            if (profile is null)
                return Result<IReadOnlyList<AddressDto>>.Failure(UserProfileErrors.ProfileNotFound);

            return Result<IReadOnlyList<AddressDto>>.Success(
                profile.Addresses.Select(ProfileMappers.ToAddressDto).ToList());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error loading addresses for subject {Subject}", request.KeycloakSubject);
            return Result<IReadOnlyList<AddressDto>>.Failure(UserProfileErrors.UnknownError);
        }
    }
}
