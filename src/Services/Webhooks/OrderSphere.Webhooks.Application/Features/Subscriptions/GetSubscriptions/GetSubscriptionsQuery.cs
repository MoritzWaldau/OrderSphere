namespace OrderSphere.Webhooks.Application.Features.Subscriptions.GetSubscriptions;

public sealed record GetSubscriptionsQuery(Guid CustomerId)
    : IQuery<Result<IReadOnlyList<SubscriptionDto>>>;
