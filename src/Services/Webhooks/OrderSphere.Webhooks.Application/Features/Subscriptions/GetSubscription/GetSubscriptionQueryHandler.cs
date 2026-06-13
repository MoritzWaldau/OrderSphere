using OrderSphere.BuildingBlocks.StronglyTypedIds;

namespace OrderSphere.Webhooks.Application.Features.Subscriptions.GetSubscription;

public sealed class GetSubscriptionQueryHandler(IWebhooksDbContext context)
    : IQueryHandler<GetSubscriptionQuery, Result<SubscriptionDto>>
{
    public async Task<Result<SubscriptionDto>> Handle(
        GetSubscriptionQuery request, CancellationToken cancellationToken)
    {
        var sub = await context.Subscriptions
            .Where(s => s.Id == WebhookSubscriptionId.From(request.Id)
                     && s.CustomerId == CustomerId.From(request.CustomerId))
            .Select(s => new SubscriptionDto(
                s.Id.Value, s.Url, s.Events, s.IsActive, s.CreatedAt, s.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        return sub is null
            ? Result<SubscriptionDto>.Failure(WebhookErrors.NotFound)
            : Result<SubscriptionDto>.Success(sub);
    }
}
