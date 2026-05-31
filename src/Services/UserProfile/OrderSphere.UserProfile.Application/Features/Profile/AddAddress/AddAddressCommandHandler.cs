namespace OrderSphere.UserProfile.Application.Features.Profile.AddAddress;

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
        RuleFor(x => x.Street).NotEmpty().MaximumLength(300);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PostalCode).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Country).NotEmpty().MaximumLength(100);
    }
}

public sealed class AddAddressCommandHandler(IUserProfileDbContext context)
    : ICommandHandler<AddAddressCommand, Result<AddressDto>>
{
    public async Task<Result<AddressDto>> Handle(AddAddressCommand request, CancellationToken ct)
    {
        var profile = await context.CustomerProfiles
            .Include(p => p.Addresses)
            .FirstOrDefaultAsync(p => p.KeycloakSubject == request.KeycloakSubject, ct);

        if (profile is null)
            return Result<AddressDto>.Failure(UserProfileErrors.ProfileNotFound);

        if (profile.Addresses.Count >= CustomerProfile.MaxAddresses)
            return Result<AddressDto>.Failure(UserProfileErrors.AddressLimitExceeded);

        var address = profile.AddAddress(
            request.Label, request.FirstName, request.LastName,
            request.Street, request.City, request.PostalCode, request.Country,
            request.SetAsDefault);

        await context.SaveChangesAsync(ct);

        return Result<AddressDto>.Success(ProfileMappers.ToAddressDto(address));
    }
}
