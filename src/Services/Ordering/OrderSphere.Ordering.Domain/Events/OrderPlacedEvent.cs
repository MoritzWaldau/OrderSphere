namespace OrderSphere.Ordering.Domain.Events;

/// <summary>
/// Published to Service Bus after an order is successfully persisted.
/// Consumed by Notification.Worker to send the confirmation email.
/// Self-contained — no downstream service calls required by the consumer.
/// </summary>
public sealed record OrderPlacedEvent(
    Guid OrderId,
    Guid CorrelationId,
    string CustomerEmail,
    string CustomerName,
    string TrackingNumber,
    string ShippingFirstName,
    string ShippingLastName,
    string ShippingStreet,
    string ShippingCity,
    string ShippingPostalCode,
    string ShippingCountry,
    IReadOnlyList<OrderPlacedItem> Items,
    decimal Total);

public sealed record OrderPlacedItem(string ProductName, int Quantity, decimal Price);
