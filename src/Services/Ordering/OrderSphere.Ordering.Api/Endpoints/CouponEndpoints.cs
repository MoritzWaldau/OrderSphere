using MediatR;
using OrderSphere.Ordering.Api.Configuration;
using OrderSphere.Ordering.Application.Features.Coupon;
using OrderSphere.Ordering.Application.Features.Coupon.Admin.CreateCoupon;
using OrderSphere.Ordering.Application.Features.Coupon.Admin.DeactivateCoupon;
using OrderSphere.Ordering.Application.Features.Coupon.Admin.GetAllCoupons;
using OrderSphere.Ordering.Application.Features.Coupon.Admin.UpdateCoupon;
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

        var admin = v1.MapGroup("admin/coupons").RequireAuthorization(AuthorizationPolicies.Admin);

        admin.MapGet("/",
            async (IMediator mediator, CancellationToken ct) =>
            {
                var result = await mediator.Send(new GetAllCouponsQuery(), ct);
                return result.ToHttpResult();
            });

        admin.MapPost("/",
            async (CreateCouponCommand command, IMediator mediator, CancellationToken ct) =>
            {
                var result = await mediator.Send(command, ct);
                return result.ToHttpResult(id => Results.Created($"/api/v1/admin/coupons/{id}", new { Id = id }));
            });

        admin.MapPut("/{id:guid}",
            async (Guid id, UpdateCouponRequest body, IMediator mediator, CancellationToken ct) =>
            {
                var result = await mediator.Send(
                    new UpdateCouponCommand(id, body.DiscountType, body.Value, body.MinSubtotal,
                        body.ValidFrom, body.ValidUntil, body.MaxRedemptions, body.IsActive), ct);
                return result.ToHttpResult(() => Results.Ok());
            });

        admin.MapPost("/{id:guid}/deactivate",
            async (Guid id, IMediator mediator, CancellationToken ct) =>
            {
                var result = await mediator.Send(new DeactivateCouponCommand(id), ct);
                return result.ToHttpResult(() => Results.Ok());
            });
    }

    public sealed record UpdateCouponRequest(
        int DiscountType,
        decimal Value,
        decimal? MinSubtotal,
        DateTime? ValidFrom,
        DateTime? ValidUntil,
        int? MaxRedemptions,
        bool IsActive);
}
