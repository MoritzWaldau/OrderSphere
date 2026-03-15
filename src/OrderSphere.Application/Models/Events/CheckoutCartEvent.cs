namespace OrderSphere.Application.Models.Events;

public sealed record CheckoutCartEvent(
    CheckoutCartDto CheckoutCart,
    List<OrderItemDto> Items
    );
