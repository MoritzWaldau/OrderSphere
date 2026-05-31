namespace OrderSphere.Webhooks.Application.Features.Subscriptions.DeactivateSubscription;

public sealed record DeactivateSubscriptionCommand(Guid Id, Guid CustomerId)
    : ICommand<Result>;
