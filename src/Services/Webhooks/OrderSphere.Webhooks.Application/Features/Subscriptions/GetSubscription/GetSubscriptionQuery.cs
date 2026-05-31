namespace OrderSphere.Webhooks.Application.Features.Subscriptions.GetSubscription;

public sealed record GetSubscriptionQuery(Guid Id, Guid CustomerId)
    : IQuery<Result<SubscriptionDto>>;
