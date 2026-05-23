namespace OrderSphere.Web.Models;

// Cart
public sealed record CartDto(Guid CustomerId, List<CartItemDto> Items);
public sealed record CartItemDto(Guid ProductId, string ProductName, decimal Price, int Quantity);

// Orders
public sealed record OrderDto(
    Guid Id,
    Guid CustomerId,
    string Status,
    string PaymentMethod,
    string? TrackingNumber,
    OrderShippingAddressDto ShippingAddress,
    List<OrderLineDto> Items,
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

// Checkout
// CustomerId, CustomerEmail, and CustomerName are derived from the JWT token server-side.
public sealed record CheckoutRequest(
    CheckoutAddress ShippingAddress,
    int PaymentMethod);

public sealed record CheckoutAddress(
    string FirstName,
    string LastName,
    string Street,
    string City,
    string PostalCode,
    string Country);

// Coupon
public sealed record CouponValidationDto(bool IsValid, decimal DiscountAmount, string? Message);
