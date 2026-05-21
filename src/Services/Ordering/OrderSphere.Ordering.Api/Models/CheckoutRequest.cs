using OrderSphere.Ordering.Domain.Enums;
using OrderSphere.Ordering.Domain.ValueObjects;

namespace OrderSphere.Ordering.Api.Models;

public sealed record CheckoutRequest(
    Guid CustomerId,
    string CustomerEmail,
    string CustomerName,
    Address ShippingAddress,
    PaymentMethod PaymentMethod);
