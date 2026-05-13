using OrderSphere.Application.Models;
using OrderSphere.Domain.Abstraction;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Application.Features.Coupon.ValidateCoupon;

public sealed record ValidateCouponQuery(string Code, decimal Subtotal)
    : IQuery<Result<CouponValidationDto>>;
