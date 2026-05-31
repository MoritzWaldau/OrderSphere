using OrderSphere.BuildingBlocks.StronglyTypedIds;

namespace OrderSphere.Webhooks.Application.Features.Subscriptions.UpdateSubscription;

public sealed class UpdateSubscriptionCommandHandler(IWebhooksDbContext context)
    : ICommandHandler<UpdateSubscriptionCommand, Result>
{
    public async Task<Result> Handle(
        UpdateSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var sub = await context.Subscriptions
            .FirstOrDefaultAsync(
                s => s.Id == WebhookSubscriptionId.From(request.Id)
                  && s.CustomerId == CustomerId.From(request.CustomerId)
                  && !s.IsDeleted,
                cancellationToken);

        if (sub is null)
            return Result.Failure(WebhookErrors.NotFound);

        sub.Update(
            request.Url,
            request.Secret ?? sub.Secret,
            request.Events);

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
