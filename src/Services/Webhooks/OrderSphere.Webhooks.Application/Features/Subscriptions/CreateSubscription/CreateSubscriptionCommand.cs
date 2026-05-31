using OrderSphere.Webhooks.Domain.Enums;

namespace OrderSphere.Webhooks.Application.Features.Subscriptions.CreateSubscription;

public sealed record CreateSubscriptionCommand(
    Guid CustomerId,
    string Url,
    string? Secret,
    WebhookEventType[] Events)
    : ICommand<Result<SubscriptionCreatedDto>>;
