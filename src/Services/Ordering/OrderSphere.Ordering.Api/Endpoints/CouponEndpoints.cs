using MediatR;
using OrderSphere.Ordering.Api.Features.Coupon;
using OrderSphere.ServiceDefaults;

namespace OrderSphere.Ordering.Api.Endpoints;

public static class CouponEndpoints
{
    public static void MapCouponEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/coupon/validate",
            async (string code, decimal subtotal, IMediator mediator, CancellationToken ct) =>
            {
                var result = await mediator.Send(new ValidateCouponQuery(code, subtotal), ct);
                return result.ToHttpResult();
            }).RequireAuthorization();
    }
}
