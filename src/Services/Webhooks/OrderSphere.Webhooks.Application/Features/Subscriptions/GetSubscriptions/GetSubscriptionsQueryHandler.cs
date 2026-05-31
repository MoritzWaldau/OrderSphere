using OrderSphere.BuildingBlocks.StronglyTypedIds;

namespace OrderSphere.Webhooks.Application.Features.Subscriptions.GetSubscriptions;

public sealed class GetSubscriptionsQueryHandler(IWebhooksDbContext context)
    : IQueryHandler<GetSubscriptionsQuery, Result<IReadOnlyList<SubscriptionDto>>>
{
    public async Task<Result<IReadOnlyList<SubscriptionDto>>> Handle(
        GetSubscriptionsQuery request, CancellationToken cancellationToken)
    {
        var customerId = CustomerId.From(request.CustomerId);

        var subs = await context.Subscriptions
            .Where(s => s.CustomerId == customerId && !s.IsDeleted)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new SubscriptionDto(
                s.Id.Value, s.Url, s.Events, s.IsActive, s.CreatedAt, s.UpdatedAt))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<SubscriptionDto>>.Success(subs);
    }
}
