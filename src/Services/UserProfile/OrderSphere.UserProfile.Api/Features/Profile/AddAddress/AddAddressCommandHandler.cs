using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.UserProfile.Api.Features.Profile;
using OrderSphere.UserProfile.Api.Models;
using OrderSphere.UserProfile.Domain.Errors;
using OrderSphere.UserProfile.Infrastructure.Persistence;

namespace OrderSphere.UserProfile.Api.Features.Profile.AddAddress;

public sealed record AddAddressCommand(
    string KeycloakSubject,
    string Label,
    string FirstName,
    string LastName,
    string Street,
    string City,
    string PostalCode,
    string Country,
    bool SetAsDefault = false) : ICommand<Result<AddressDto>>;

public sealed class AddAddressCommandValidator : AbstractValidator<AddAddressCommand>
{
    public AddAddressCommandValidator()
    {
        RuleFor(x => x.KeycloakSubject).NotEmpty();
        RuleFor(x => x.Label).NotEmpty().MaximumLength(100);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Street).NotEmpty().MaximumLength(200);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PostalCode).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Country).NotEmpty().MaximumLength(100);
    }
}

public sealed class AddAddressCommandHandler(
    UserProfileDbContext context,
    ILogger<AddAddressCommandHandler> logger
) : ICommandHandler<AddAddressCommand, Result<AddressDto>>
{
    private const int MaxAddresses = 10;

    public async Task<Result<AddressDto>> Handle(AddAddressCommand request, CancellationToken ct)
    {
        try
        {
            var profile = await context.CustomerProfiles
                .Include(p => p.Addresses)
                .FirstOrDefaultAsync(p => p.KeycloakSubject == request.KeycloakSubject, ct);

            if (profile is null)
                return Result<AddressDto>.Failure(UserProfileErrors.ProfileNotFound);

            if (profile.Addresses.Count >= MaxAddresses)
                return Result<AddressDto>.Failure(UserProfileErrors.AddressLimitExceeded);

            var address = profile.AddAddress(
                request.Label, request.FirstName, request.LastName,
                request.Street, request.City, request.PostalCode, request.Country,
                request.SetAsDefault);

            await context.SaveChangesAsync(ct);

            return Result<AddressDto>.Success(ProfileMappers.ToAddressDto(address));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error adding address for subject {Subject}", request.KeycloakSubject);
            return Result<AddressDto>.Failure(UserProfileErrors.UnknownError);
        }
    }
}
