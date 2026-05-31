using OrderSphere.Ordering.Domain.Enums;

namespace OrderSphere.Ordering.Application.Models;

public sealed record OrderDto(
    Guid Id,
    Guid CustomerId,
    OrderStatus Status,
    PaymentMethod PaymentMethod,
    string? TrackingNumber,
    OrderShippingAddressDto ShippingAddress,
    IReadOnlyList<OrderLineDto> Items,
    decimal Total,
    DateTime CreatedAt);

public sealed record OrderShippingAddressDto(
    string FirstName,
    string LastName,
    string Street,
    string City,
    string PostalCode,
    string Country);

public sealed record OrderLineDto(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal Price);
