using OrderSphere.Domain.Enums;
using OrderSphere.Domain.ValueObjects;

namespace OrderSphere.Application.Models;

public sealed record CheckoutCartDto(
    Guid CustomerId,
    Address ShippingAddress,
    PaymentMethod PaymentMethod
);
