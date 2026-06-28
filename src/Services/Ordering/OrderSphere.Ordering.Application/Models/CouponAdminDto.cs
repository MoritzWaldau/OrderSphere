namespace OrderSphere.Ordering.Application.Models;

public sealed record CouponAdminDto(
    Guid Id,
    string Code,
    int DiscountType,   // 0 = Flat, 1 = Percentage, 2 = Tiered
    decimal Value,
    decimal? MinSubtotal,
    DateTime? ValidFrom,
    DateTime? ValidUntil,
    int? MaxRedemptions,
    int RedeemedCount,
    bool IsActive,
    IReadOnlyList<CouponTierDto> Tiers,
    IReadOnlyList<Guid> ScopedCategoryIds);

public sealed record CouponTierDto(decimal MinSubtotal, decimal DiscountValue);
