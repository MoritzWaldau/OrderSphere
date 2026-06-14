namespace OrderSphere.UserProfile.Application.Features.Profile.SetDefaultAddress;

public sealed record SetDefaultAddressCommand(
    string Subject,
    Guid AddressId) : ICommand<Result>;

public sealed class SetDefaultAddressCommandValidator : AbstractValidator<SetDefaultAddressCommand>
{
    public SetDefaultAddressCommandValidator()
    {
        RuleFor(x => x.Subject).NotEmpty();
        RuleFor(x => x.AddressId).NotEmpty();
    }
}

public sealed class SetDefaultAddressCommandHandler(IUserProfileDbContext context)
    : ICommandHandler<SetDefaultAddressCommand, Result>
{
    public async Task<Result> Handle(SetDefaultAddressCommand request, CancellationToken ct)
    {
        var profile = await context.CustomerProfiles
            .Include(p => p.Addresses)
            .FirstOrDefaultAsync(p => p.Subject == request.Subject, ct);

        if (profile is null)
            return Result.Failure(UserProfileErrors.ProfileNotFound);

        var success = profile.SetDefaultAddress(SavedAddressId.From(request.AddressId));
        if (!success)
            return Result.Failure(UserProfileErrors.AddressNotFound);

        await context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
