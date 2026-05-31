using OrderSphere.BuildingBlocks.StronglyTypedIds;

namespace OrderSphere.Webhooks.Application.Features.Subscriptions.ActivateSubscription;

public sealed class ActivateSubscriptionCommandHandler(IWebhooksDbContext context)
    : ICommandHandler<ActivateSubscriptionCommand, Result>
{
    public async Task<Result> Handle(
        ActivateSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var sub = await context.Subscriptions
            .FirstOrDefaultAsync(
                s => s.Id == WebhookSubscriptionId.From(request.Id)
                  && s.CustomerId == CustomerId.From(request.CustomerId)
                  && !s.IsDeleted,
                cancellationToken);

        if (sub is null)
            return Result.Failure(WebhookErrors.NotFound);

        sub.Activate();
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
