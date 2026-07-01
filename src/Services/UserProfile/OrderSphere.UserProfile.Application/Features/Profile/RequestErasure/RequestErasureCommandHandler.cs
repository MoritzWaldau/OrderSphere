using System.Text.Json;
using OrderSphere.BuildingBlocks.Contracts.Events;

namespace OrderSphere.UserProfile.Application.Features.Profile.RequestErasure;

public sealed record RequestErasureCommand(string Subject) : ICommand<Result>;

public sealed class RequestErasureCommandValidator : AbstractValidator<RequestErasureCommand>
{
    public RequestErasureCommandValidator()
    {
        RuleFor(x => x.Subject).NotEmpty();
    }
}

/// <summary>
/// D1 — GDPR right-to-erasure. UserProfile is the system of record for the customer, so it
/// anonymizes its own profile/addresses synchronously, then stages
/// <see cref="CustomerErasureRequestedIntegrationEvent"/> in the outbox for every other
/// PII-holding service to consume from its own fan-out queue.
/// </summary>
public sealed class RequestErasureCommandHandler(IUserProfileDbContext context)
    : ICommandHandler<RequestErasureCommand, Result>
{
    public async Task<Result> Handle(RequestErasureCommand request, CancellationToken ct)
    {
        var profile = await context.CustomerProfiles
            .Include(p => p.Addresses)
            .FirstOrDefaultAsync(p => p.Subject == request.Subject, ct);

        if (profile is null)
            return Result.Failure(UserProfileErrors.ProfileNotFound);

        var email = profile.Email;
        profile.Anonymize();

        var evt = new CustomerErasureRequestedIntegrationEvent
        {
            CustomerSub = request.Subject,
            CustomerEmail = email,
        };
        context.AddOutboxMessage(nameof(CustomerErasureRequestedIntegrationEvent), JsonSerializer.Serialize(evt));

        await context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
