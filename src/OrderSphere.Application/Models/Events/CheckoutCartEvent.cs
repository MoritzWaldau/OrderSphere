namespace OrderSphere.Application.Models.Events;

public sealed record CheckoutCartEvent(
    Guid CorrelationId,
    CheckoutCartDto CheckoutCart,
    List<OrderItemDto> Items
    );
