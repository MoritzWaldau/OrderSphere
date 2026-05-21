namespace OrderSphere.Notification.Worker.Events;

/// <summary>
/// Consumed from the "notification-orders" Service Bus queue.
/// Published by Ordering.Worker after an order is successfully created.
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
