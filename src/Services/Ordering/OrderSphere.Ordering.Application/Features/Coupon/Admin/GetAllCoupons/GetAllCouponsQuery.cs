using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.Ordering.Application.Models;

namespace OrderSphere.Ordering.Application.Features.Coupon.Admin.GetAllCoupons;

public sealed record GetAllCouponsQuery : IQuery<Result<List<CouponAdminDto>>>;
