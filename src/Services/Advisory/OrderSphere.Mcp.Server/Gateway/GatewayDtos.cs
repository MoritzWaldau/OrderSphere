namespace OrderSphere.Mcp.Server.Gateway;

// Lightweight DTOs mirroring the public /api/v1 JSON contracts exposed via the API Gateway.
// Kept local so the MCP server stays decoupled from the frontend and service projects.

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);

public sealed record ProductDto(
    Guid Id,
    string Name,
    string Slug,
    string Description,
    decimal Price,
    int Stock,
    Guid CategoryId,
    string CategoryName,
    string SKU,
    string? ImageUrl,
    bool IsActive);

public sealed record CategoryDto(Guid Id, string Name, string Description, int ProductCount);

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

public sealed record OrderLineDto(Guid ProductId, string ProductName, int Quantity, decimal Price);

public sealed record CouponValidationDto(bool IsValid, decimal DiscountAmount, string? Message);

public sealed record CartDto(Guid CustomerId, List<CartItemDto> Items);

public sealed record CartItemDto(Guid ProductId, string ProductName, decimal Price, int Quantity);

public sealed record PaymentDto(
    Guid Id,
    Guid OrderId,
    decimal Amount,
    string Currency,
    string PaymentMethod,
    string Status,
    string? TransactionId,
    string? FailureReason,
    DateTime CreatedAt);

public sealed record ProfileDto(
    Guid Id,
    string Subject,
    string DisplayName,
    string Email,
    bool DarkModeEnabled,
    List<AddressDto> Addresses);

public sealed record AddressDto(
    Guid Id,
    string Label,
    string FirstName,
    string LastName,
    string Street,
    string City,
    string PostalCode,
    string Country,
    bool IsDefault);

public sealed record CartMutationResult(bool Success, string? Error);
