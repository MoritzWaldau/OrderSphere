namespace OrderSphere.Webhooks.Application.Features.Subscriptions.ActivateSubscription;

public sealed record ActivateSubscriptionCommand(Guid Id, Guid CustomerId)
    : ICommand<Result>;
