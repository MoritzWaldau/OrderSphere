namespace OrderSphere.Ordering.Application.Models;

public sealed record CouponAdminDto(
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
