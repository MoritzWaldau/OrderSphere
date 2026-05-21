using MediatR;
using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.Ordering.Api.Models;

namespace OrderSphere.Ordering.Api.Features.Coupon;

public sealed record ValidateCouponQuery(string Code, decimal Subtotal)
    : IRequest<Result<CouponValidationDto>>;
