using MediatR;
using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.Ordering.Domain.Enums;
using OrderSphere.Ordering.Domain.ValueObjects;

namespace OrderSphere.Ordering.Api.Features.Checkout;

/// <summary>
/// Initiates cart-to-order checkout for the authenticated customer.
/// Identity fields (<see cref="CustomerId"/>, <see cref="CustomerEmail"/>,
/// <see cref="CustomerName"/>) are populated from the JWT token by the endpoint —
/// the client body carries only shipping and payment data.
/// </summary>
public sealed record CheckoutCartCommand(
    Guid CustomerId,
    string CustomerEmail,
    string CustomerName,
    Address ShippingAddress,
    PaymentMethod PaymentMethod
) : IRequest<Result<Guid>>;
