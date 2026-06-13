using MediatR;
using OrderSphere.Ordering.Application.Features.Coupon;
using OrderSphere.ServiceDefaults;

namespace OrderSphere.Ordering.Api.Endpoints;

public static class CouponEndpoints
{
    public static void MapCouponEndpoints(this RouteGroupBuilder v1)
    {
        v1.MapGet("coupons/validate",
            async (string code, decimal subtotal, IMediator mediator, CancellationToken ct) =>
            {
                var result = await mediator.Send(new ValidateCouponQuery(code, subtotal), ct);
                return result.ToHttpResult();
            }).RequireAuthorization();
    }
}
