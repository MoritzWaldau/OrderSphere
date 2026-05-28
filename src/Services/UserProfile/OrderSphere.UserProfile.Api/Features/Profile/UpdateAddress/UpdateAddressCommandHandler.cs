using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.BuildingBlocks.StronglyTypedIds;
using OrderSphere.UserProfile.Api.Features.Profile;
using OrderSphere.UserProfile.Api.Models;
using OrderSphere.UserProfile.Domain.Errors;
using OrderSphere.UserProfile.Infrastructure.Persistence;

namespace OrderSphere.UserProfile.Api.Features.Profile.UpdateAddress;

public sealed record UpdateAddressCommand(
    string KeycloakSubject,
    Guid AddressId,
    string Label,
    string FirstName,
    string LastName,
    string Street,
    string City,
    string PostalCode,
    string Country) : ICommand<Result<AddressDto>>;

public sealed class UpdateAddressCommandValidator : AbstractValidator<UpdateAddressCommand>
{
    public UpdateAddressCommandValidator()
    {
        RuleFor(x => x.KeycloakSubject).NotEmpty();
        RuleFor(x => x.AddressId).NotEmpty();
        RuleFor(x => x.Label).NotEmpty().MaximumLength(100);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Street).NotEmpty().MaximumLength(200);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PostalCode).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Country).NotEmpty().MaximumLength(100);
    }
}

public sealed class UpdateAddressCommandHandler(
    UserProfileDbContext context,
    ILogger<UpdateAddressCommandHandler> logger
) : ICommandHandler<UpdateAddressCommand, Result<AddressDto>>
{
    public async Task<Result<AddressDto>> Handle(UpdateAddressCommand request, CancellationToken ct)
    {
        try
        {
            var profile = await context.CustomerProfiles
                .Include(p => p.Addresses)
                .FirstOrDefaultAsync(p => p.KeycloakSubject == request.KeycloakSubject, ct);

            if (profile is null)
                return Result<AddressDto>.Failure(UserProfileErrors.ProfileNotFound);

            var address = profile.Addresses.FirstOrDefault(
                a => a.Id == SavedAddressId.From(request.AddressId));

            if (address is null)
                return Result<AddressDto>.Failure(UserProfileErrors.AddressNotFound);

            address.Update(
                request.Label, request.FirstName, request.LastName,
                request.Street, request.City, request.PostalCode, request.Country);

            await context.SaveChangesAsync(ct);

            return Result<AddressDto>.Success(ProfileMappers.ToAddressDto(address));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating address {AddressId} for subject {Subject}",
                request.AddressId, request.KeycloakSubject);
            return Result<AddressDto>.Failure(UserProfileErrors.UnknownError);
        }
    }
}
