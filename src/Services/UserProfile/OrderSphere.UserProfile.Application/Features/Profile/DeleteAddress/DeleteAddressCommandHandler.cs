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

public sealed class DeleteAddressCommandHandler(IUserProfileDbContext context)
    : ICommandHandler<DeleteAddressCommand, Result>
{
    public async Task<Result> Handle(DeleteAddressCommand request, CancellationToken ct)
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
}
