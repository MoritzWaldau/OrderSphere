using Microsoft.EntityFrameworkCore;
using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.Ordering.Application.Abstractions;
using OrderSphere.Ordering.Domain.Enums;
using OrderSphere.Ordering.Domain.Errors;
using OrderSphere.Ordering.Domain.ValueObjects;
using Entities = OrderSphere.Ordering.Domain.Entities;

namespace OrderSphere.Ordering.Application.Features.Coupon.Admin.CreateCoupon;

public sealed class CreateCouponCommandHandler(IOrderingDbContext context)
    : ICommandHandler<CreateCouponCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateCouponCommand request, CancellationToken ct)
    {
        var code = Entities.Coupon.Normalize(request.Code);

        var exists = await context.Coupons.AnyAsync(c => c.Code == code, ct);
        if (exists)
            return Result<Guid>.Failure(CouponErrors.CodeExists);

        var tiers = request.Tiers?.Select(t => new CouponTier(t.MinSubtotal, t.DiscountValue));

        var coupon = new Entities.Coupon(
            request.Code,
            (DiscountType)request.DiscountType,
            request.Value,
            request.MinSubtotal,
            request.ValidFrom,
            request.ValidUntil,
            request.MaxRedemptions,
            request.IsActive,
            tiers,
            request.ScopedCategoryIds);

        context.Coupons.Add(coupon);
        await context.SaveChangesAsync(ct);

        return Result<Guid>.Success(coupon.Id.Value);
    }
}
