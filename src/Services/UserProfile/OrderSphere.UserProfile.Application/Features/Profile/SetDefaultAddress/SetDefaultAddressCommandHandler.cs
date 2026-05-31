using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.BuildingBlocks.StronglyTypedIds;
using OrderSphere.UserProfile.Domain.Errors;
using OrderSphere.UserProfile.Infrastructure.Persistence;

namespace OrderSphere.UserProfile.Api.Features.Profile.SetDefaultAddress;

public sealed record SetDefaultAddressCommand(
    string KeycloakSubject,
    Guid AddressId) : ICommand<Result>;

public sealed class SetDefaultAddressCommandValidator : AbstractValidator<SetDefaultAddressCommand>
{
    public SetDefaultAddressCommandValidator()
    {
        RuleFor(x => x.KeycloakSubject).NotEmpty();
        RuleFor(x => x.AddressId).NotEmpty();
    }
}

public sealed class SetDefaultAddressCommandHandler(
    UserProfileDbContext context,
    ILogger<SetDefaultAddressCommandHandler> logger
) : ICommandHandler<SetDefaultAddressCommand, Result>
{
    public async Task<Result> Handle(SetDefaultAddressCommand request, CancellationToken ct)
    {
        try
        {
            var profile = await context.CustomerProfiles
                .Include(p => p.Addresses)
                .FirstOrDefaultAsync(p => p.KeycloakSubject == request.KeycloakSubject, ct);

            if (profile is null)
                return Result.Failure(UserProfileErrors.ProfileNotFound);

            var success = profile.SetDefaultAddress(SavedAddressId.From(request.AddressId));
            if (!success)
                return Result.Failure(UserProfileErrors.AddressNotFound);

            await context.SaveChangesAsync(ct);
            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error setting default address {AddressId} for subject {Subject}",
                request.AddressId, request.KeycloakSubject);
            return Result.Failure(UserProfileErrors.UnknownError);
        }
    }
}
