namespace OrderSphere.Ordering.Api.Models;

public sealed record CouponValidationDto(
    string Code,
    bool IsValid,
    decimal DiscountAmount,
    string? Message);
