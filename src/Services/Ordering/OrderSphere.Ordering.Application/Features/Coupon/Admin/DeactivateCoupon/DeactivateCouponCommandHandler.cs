using Microsoft.EntityFrameworkCore;
using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.BuildingBlocks.StronglyTypedIds;
using OrderSphere.Ordering.Application.Abstractions;
using OrderSphere.Ordering.Domain.Errors;

namespace OrderSphere.Ordering.Application.Features.Coupon.Admin.DeactivateCoupon;

public sealed class DeactivateCouponCommandHandler(IOrderingDbContext context)
    : ICommandHandler<DeactivateCouponCommand, Result>
{
    public async Task<Result> Handle(DeactivateCouponCommand request, CancellationToken ct)
    {
        var coupon = await context.Coupons
            .AsTracking()
            .FirstOrDefaultAsync(c => c.Id == CouponId.From(request.Id), ct);

        if (coupon is null)
            return Result.Failure(CouponErrors.NotFound);

        coupon.Deactivate();
        await context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
