using OrderSphere.Domain.ValueObjects;

namespace OrderSphere.Application.Models;

public sealed record OrderConfirmationData(
    Guid OrderId,
    string TrackingNumber,
    Address ShippingAddress,
    IReadOnlyList<OrderConfirmationLine> Items,
    decimal Total);

public sealed record OrderConfirmationLine(
    string ProductName,
    int Quantity,
    decimal Price);
