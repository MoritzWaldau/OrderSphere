using OrderSphere.Ordering.Domain.Enums;
using OrderSphere.Ordering.Domain.ValueObjects;

namespace OrderSphere.Ordering.Api.Models;

/// <summary>
/// Request body for <c>POST /api/v1/checkout</c>.
/// Identity fields (customer ID, e-mail, name) are derived from the authenticated
/// JWT token; only shipping and payment data are submitted by the client.
/// </summary>
public sealed record CheckoutRequest(
    Address ShippingAddress,
    PaymentMethod PaymentMethod);
