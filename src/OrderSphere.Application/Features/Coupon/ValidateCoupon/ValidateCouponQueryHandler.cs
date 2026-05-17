using Microsoft.Extensions.Logging;
using OrderSphere.Application.Models;
using OrderSphere.Domain.Abstraction;
using OrderSphere.Domain.Errors;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Application.Features.Coupon.ValidateCoupon;

public sealed class ValidateCouponQueryHandler(
    ILogger<ValidateCouponQueryHandler> logger)
    : IQueryHandler<ValidateCouponQuery, Result<CouponValidationDto>>
{
    // Coupons live in code until we add a Coupon table. Centralized here so the
    // rule is server-authoritative and not scattered across the UI.
    private static readonly IReadOnlyDictionary<string, CouponDefinition> KnownCoupons =
        new Dictionary<string, CouponDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            ["WELCOME10"] = new CouponDefinition(FlatAmount: 10m),
            ["SUMMER15"] = new CouponDefinition(FlatAmount: 15m, MinSubtotal: 100m)
        };

    public Task<Result<CouponValidationDto>> Handle(ValidateCouponQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
        {
            return Task.FromResult(Result<CouponValidationDto>.Failure(CouponErrors.CodeRequired));
        }

        if (!KnownCoupons.TryGetValue(request.Code.Trim(), out var coupon))
        {
            logger.LogInformation("Coupon code not found: {Code}", request.Code);
            return Task.FromResult(Result<CouponValidationDto>.Failure(CouponErrors.InvalidCode));
        }

        if (coupon.MinSubtotal is { } minSubtotal && request.Subtotal < minSubtotal)
        {
            return Task.FromResult(Result<CouponValidationDto>.Success(new CouponValidationDto(
                request.Code,
                IsValid: false,
                DiscountAmount: 0m,
                Message: $"Gutschein gilt erst ab einem Bestellwert von {minSubtotal:0.00} €.")));
        }

        var discount = Math.Min(coupon.FlatAmount, request.Subtotal);

        return Task.FromResult(Result<CouponValidationDto>.Success(new CouponValidationDto(
            request.Code,
            IsValid: true,
            DiscountAmount: discount,
            Message: "Gutschein wurde angewendet.")));
    }

    private sealed record CouponDefinition(decimal FlatAmount, decimal? MinSubtotal = null);
}
