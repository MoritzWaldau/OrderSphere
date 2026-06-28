using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.Ordering.Application.Features.Coupon.Admin.CreateCoupon;

namespace OrderSphere.Ordering.Application.Features.Coupon.Admin.UpdateCoupon;

// Code is immutable after creation; everything else is editable.
public sealed record UpdateCouponCommand(
    Guid Id,
    int DiscountType,
    decimal Value,
    decimal? MinSubtotal,
    DateTime? ValidFrom,
    DateTime? ValidUntil,
    int? MaxRedemptions,
    bool IsActive,
    IReadOnlyList<CreateCouponTierDto>? Tiers = null,
    IReadOnlyList<Guid>? ScopedCategoryIds = null) : ICommand<Result>;
