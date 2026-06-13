using OrderSphere.BuildingBlocks.StronglyTypedIds;

namespace OrderSphere.Webhooks.Application.Features.Subscriptions.DeleteSubscription;

public sealed class DeleteSubscriptionCommandHandler(IWebhooksDbContext context)
    : ICommandHandler<DeleteSubscriptionCommand, Result>
{
    public async Task<Result> Handle(
        DeleteSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var sub = await context.Subscriptions
            .FirstOrDefaultAsync(
                s => s.Id == WebhookSubscriptionId.From(request.Id)
                  && s.CustomerId == CustomerId.From(request.CustomerId),
                cancellationToken);

        if (sub is null)
            return Result.Failure(WebhookErrors.NotFound);

        sub.Delete();
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
