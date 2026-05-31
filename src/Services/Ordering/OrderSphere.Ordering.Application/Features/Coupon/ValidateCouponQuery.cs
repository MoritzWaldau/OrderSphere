using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.Ordering.Application.Models;

namespace OrderSphere.Ordering.Application.Features.Coupon;

public sealed record ValidateCouponQuery(string Code, decimal Subtotal)
    : IQuery<Result<CouponValidationDto>>;
