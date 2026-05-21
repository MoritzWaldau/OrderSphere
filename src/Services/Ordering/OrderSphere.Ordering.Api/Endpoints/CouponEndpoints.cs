using MediatR;
using OrderSphere.Ordering.Api.Features.Coupon;
using OrderSphere.Ordering.Api.Models;

namespace OrderSphere.Ordering.Api.Endpoints;

public static class CouponEndpoints
{
    public static void MapCouponEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/coupon/validate",
            async (string code, decimal subtotal, IMediator mediator, CancellationToken ct) =>
            {
                var result = await mediator.Send(new ValidateCouponQuery(code, subtotal), ct);
                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.BadRequest(new ErrorResponse(result.Error.Code, result.Error.Description));
            }).RequireAuthorization();
    }
}
