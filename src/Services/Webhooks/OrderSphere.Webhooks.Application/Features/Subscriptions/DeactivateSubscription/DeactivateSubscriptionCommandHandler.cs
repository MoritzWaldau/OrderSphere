using OrderSphere.BuildingBlocks.StronglyTypedIds;

namespace OrderSphere.Webhooks.Application.Features.Subscriptions.DeactivateSubscription;

public sealed class DeactivateSubscriptionCommandHandler(IWebhooksDbContext context)
    : ICommandHandler<DeactivateSubscriptionCommand, Result>
{
    public async Task<Result> Handle(
        DeactivateSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var sub = await context.Subscriptions
            .FirstOrDefaultAsync(
                s => s.Id == WebhookSubscriptionId.From(request.Id)
                  && s.CustomerId == CustomerId.From(request.CustomerId),
                cancellationToken);

        if (sub is null)
            return Result.Failure(WebhookErrors.NotFound);

        sub.Deactivate();
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
