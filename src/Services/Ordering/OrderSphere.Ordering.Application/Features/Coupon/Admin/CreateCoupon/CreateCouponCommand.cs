using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Ordering.Application.Features.Coupon.Admin.CreateCoupon;

public sealed record CreateCouponCommand(
    string Code,
    int DiscountType,
    decimal Value,
    decimal? MinSubtotal,
    DateTime? ValidFrom,
    DateTime? ValidUntil,
    int? MaxRedemptions,
    bool IsActive) : ICommand<Result<Guid>>;
