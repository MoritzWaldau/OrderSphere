using Microsoft.Extensions.Logging;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.BuildingBlocks.StronglyTypedIds;
using OrderSphere.UserProfile.Domain.Errors;
using OrderSphere.UserProfile.Application.Abstractions;

namespace OrderSphere.UserProfile.Application.Features.Profile.DeleteAddress;

public sealed record DeleteAddressCommand(
    string KeycloakSubject,
    Guid AddressId) : ICommand<Result>;

public sealed class DeleteAddressCommandValidator : AbstractValidator<DeleteAddressCommand>
{
    public DeleteAddressCommandValidator()
    {
        RuleFor(x => x.KeycloakSubject).NotEmpty();
        RuleFor(x => x.AddressId).NotEmpty();
    }
}

public sealed class DeleteAddressCommandHandler(
    IUserProfileDbContext context,
    ILogger<DeleteAddressCommandHandler> logger
) : ICommandHandler<DeleteAddressCommand, Result>
{
    public async Task<Result> Handle(DeleteAddressCommand request, CancellationToken ct)
    {
        try
        {
            var profile = await context.CustomerProfiles
                .Include(p => p.Addresses)
                .FirstOrDefaultAsync(p => p.KeycloakSubject == request.KeycloakSubject, ct);

            if (profile is null)
                return Result.Failure(UserProfileErrors.ProfileNotFound);

            var removed = profile.RemoveAddress(SavedAddressId.From(request.AddressId));
            if (!removed)
                return Result.Failure(UserProfileErrors.AddressNotFound);

            await context.SaveChangesAsync(ct);
            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting address {AddressId} for subject {Subject}",
                request.AddressId, request.KeycloakSubject);
            return Result.Failure(UserProfileErrors.UnknownError);
        }
    }
}
