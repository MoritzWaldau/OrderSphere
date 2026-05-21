using OrderSphere.Application.Models;
using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Application.Features.Coupon.ValidateCoupon;

public sealed record ValidateCouponQuery(string Code, decimal Subtotal)
    : IQuery<Result<CouponValidationDto>>;
