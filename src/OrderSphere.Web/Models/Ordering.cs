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

// Editable form-state for the checkout page (bound across the address/payment sub-forms).
public sealed class CheckoutFormModel
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = "Deutschland";
    public string Email { get; set; } = string.Empty;
    public int PaymentMethod { get; set; }
}
