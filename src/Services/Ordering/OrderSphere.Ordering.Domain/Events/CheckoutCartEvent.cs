using OrderSphere.Ordering.Domain.Enums;
using OrderSphere.Ordering.Domain.ValueObjects;

namespace OrderSphere.Ordering.Domain.Events;

/// <summary>
/// Published to Service Bus after a successful checkout.
/// Consumed by Ordering.Worker to create the Order entity.
/// </summary>
public sealed record CheckoutCartEvent(
    Guid CorrelationId,
    CheckoutCartDto CheckoutCart,
    List<OrderItemEventDto> Items);

public sealed record CheckoutCartDto(
    Guid CustomerId,
    string CustomerEmail,
    string CustomerName,
    Address ShippingAddress,
    PaymentMethod PaymentMethod);

public sealed record OrderItemEventDto(Guid ProductId, string ProductName, int Quantity, decimal Price);
