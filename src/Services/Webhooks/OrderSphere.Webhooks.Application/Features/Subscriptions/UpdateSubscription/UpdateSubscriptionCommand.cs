using OrderSphere.Webhooks.Domain.Enums;

namespace OrderSphere.Webhooks.Application.Features.Subscriptions.UpdateSubscription;

public sealed record UpdateSubscriptionCommand(
    Guid Id,
    Guid CustomerId,
    string Url,
    string? Secret,
    WebhookEventType[] Events)
    : ICommand<Result>;
