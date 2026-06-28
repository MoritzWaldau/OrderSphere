using Microsoft.EntityFrameworkCore;
using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.BuildingBlocks.StronglyTypedIds;
using OrderSphere.Ordering.Application.Abstractions;
using OrderSphere.Ordering.Domain.Enums;
using OrderSphere.Ordering.Domain.Errors;
using OrderSphere.Ordering.Domain.ValueObjects;

namespace OrderSphere.Ordering.Application.Features.Coupon.Admin.UpdateCoupon;

public sealed class UpdateCouponCommandHandler(IOrderingDbContext context)
    : ICommandHandler<UpdateCouponCommand, Result>
{
    public async Task<Result> Handle(UpdateCouponCommand request, CancellationToken ct)
    {
        var coupon = await context.Coupons
            .AsTracking()
            .FirstOrDefaultAsync(c => c.Id == CouponId.From(request.Id), ct);

        if (coupon is null)
            return Result.Failure(CouponErrors.NotFound);

        var tiers = request.Tiers?.Select(t => new CouponTier(t.MinSubtotal, t.DiscountValue));

        coupon.Update(
            (DiscountType)request.DiscountType,
            request.Value,
            request.MinSubtotal,
            request.ValidFrom,
            request.ValidUntil,
            request.MaxRedemptions,
            request.IsActive,
            tiers,
            request.ScopedCategoryIds);

        await context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
