using OrderSphere.Application.Abstraction;
using OrderSphere.Application.Models;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Application.Features.Coupon.ValidateCoupon;

public sealed class ValidateCouponQueryHandler(IOrderingClient orderingClient)
    : IQueryHandler<ValidateCouponQuery, Result<CouponValidationDto>>
{
    public Task<Result<CouponValidationDto>> Handle(ValidateCouponQuery request, CancellationToken cancellationToken)
        => orderingClient.ValidateCouponAsync(request.Code, request.Subtotal, cancellationToken);
}
