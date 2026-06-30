namespace OrderSphere.Web.Models;

// Catalog admin
public sealed record AdminProductDto(
    Guid Id,
    string Name,
    string Slug,
    string Description,
    decimal Price,
    int Stock,
    Guid CategoryId,
    string CategoryName,
    string SKU,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record AdminProductInput(
    string Name,
    string Description,
    decimal Price,
    int Stock,
    Guid CategoryId,
    string SKU,
    bool IsActive = true,
    string? ImageUrl = null);

public sealed record AdminCategoryDto(
    Guid Id,
    string Name,
    string Description,
    bool IsActive,
    int ProductCount,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record AdminCategoryInput(string Name, string Description, bool IsActive = true);

// Ordering admin stats
public sealed record OrderStatsDto(
    int TotalOrders,
    int OrdersToday,
    decimal TotalRevenue,
    decimal RevenueToday,
    int PendingShipments,
    int TotalCustomers,
    IReadOnlyList<RecentOrderDto> RecentOrders);

public sealed record RecentOrderDto(
    Guid Id,
    string CustomerName,
    decimal Total,
    string Status,
    DateTime CreatedAt);

// UserProfile admin
public sealed record AdminUserSummaryDto(
    Guid Id,
    string Subject,
    string DisplayName,
    string Email,
    bool DarkModeEnabled,
    int AddressCount);

public sealed record AdminUserDetailDto(
    Guid Id,
    string Subject,
    string DisplayName,
    string Email,
    bool DarkModeEnabled,
    IReadOnlyList<AddressDto> Addresses);

// Invoicing admin — support lookup by invoice number, plus the I4 adjustment/status model
// (Issued/Adjusted/CreditIssued).
public sealed record AdminInvoiceDto(
    Guid Id,
    string InvoiceNumber,
    Guid OrderId,
    string CustomerName,
    string CustomerEmail,
    decimal Total,
    decimal NetAmount,
    decimal TaxRate,
    decimal TaxAmount,
    decimal AdjustedNet,
    decimal AdjustedTax,
    decimal AdjustedTotal,
    DateTime IssuedAt,
    string Status,
    IReadOnlyList<AdminInvoiceAdjustmentDto> Adjustments);

public sealed record AdminInvoiceAdjustmentDto(
    Guid Id,
    string Type,
    decimal AmountNet,
    string Reason,
    string AppliedBy,
    DateTime AppliedAt);

// Ordering admin — coupons
public sealed record AdminCouponDto(
    Guid Id,
    string Code,
    int DiscountType,   // 0 = Flat, 1 = Percentage
    decimal Value,
    decimal? MinSubtotal,
    DateTime? ValidFrom,
    DateTime? ValidUntil,
    int? MaxRedemptions,
    int RedeemedCount,
    bool IsActive);

public sealed record AdminCouponInput(
    string Code,
    int DiscountType,
    decimal Value,
    decimal? MinSubtotal,
    DateTime? ValidFrom,
    DateTime? ValidUntil,
    int? MaxRedemptions,
    bool IsActive = true);
