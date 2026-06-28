using Microsoft.EntityFrameworkCore;
using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.Ordering.Application.Abstractions;
using OrderSphere.Ordering.Application.Models;

namespace OrderSphere.Ordering.Application.Features.Coupon.Admin.GetAllCoupons;

public sealed class GetAllCouponsQueryHandler(IOrderingDbContext context)
    : IQueryHandler<GetAllCouponsQuery, Result<List<CouponAdminDto>>>
{
    public async Task<Result<List<CouponAdminDto>>> Handle(GetAllCouponsQuery request, CancellationToken ct)
    {
        var coupons = await context.Coupons
            .AsNoTracking()
            .OrderBy(c => c.Code)
            .ToListAsync(ct);

        var dtos = coupons.Select(c => new CouponAdminDto(
                c.Id.Value,
                c.Code,
                (int)c.DiscountType,
                c.Value,
                c.MinSubtotal,
                c.ValidFrom,
                c.ValidUntil,
                c.MaxRedemptions,
                c.RedeemedCount,
                c.IsActive,
                c.Tiers.Select(t => new CouponTierDto(t.MinSubtotal, t.DiscountValue)).ToList(),
                c.ScopedCategoryIds))
            .ToList();

        return Result<List<CouponAdminDto>>.Success(dtos);
    }
}
