namespace OrderSphere.Webhooks.Application.Features.Subscriptions.DeleteSubscription;

public sealed record DeleteSubscriptionCommand(Guid Id, Guid CustomerId)
    : ICommand<Result>;
