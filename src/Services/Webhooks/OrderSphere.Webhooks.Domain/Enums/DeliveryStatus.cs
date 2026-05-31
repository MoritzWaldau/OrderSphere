namespace OrderSphere.Webhooks.Domain.Enums;

/// <summary>
/// Tracks the delivery outcome of a single webhook attempt or overall delivery.
/// </summary>
public enum DeliveryStatus
{
    Pending,
    Succeeded,
    Failed,
}
