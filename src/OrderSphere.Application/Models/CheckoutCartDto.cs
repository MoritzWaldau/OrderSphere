using OrderSphere.Domain.Enums;
using OrderSphere.Domain.ValueObjects;

namespace OrderSphere.Application.Models;

public sealed record CheckoutCartDto(
    Guid CustomerId,
    string CustomerEmail,
    string CustomerName,
    Address ShippingAddress,
    PaymentMethod PaymentMethod
);
