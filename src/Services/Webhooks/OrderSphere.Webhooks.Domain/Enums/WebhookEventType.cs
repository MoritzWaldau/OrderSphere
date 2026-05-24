namespace OrderSphere.Webhooks.Domain.Enums;

/// <summary>
/// Event types that webhook subscriptions can listen for.
/// Each maps to one or more integration events on the service bus.
/// </summary>
public enum WebhookEventType
{
    OrderPlaced,
    OrderStatusChanged,
    PaymentCompleted,
    PaymentFailed,
}
