namespace OrderSphere.Ordering.Application.Models;

public sealed record CouponValidationDto(
    string Code,
    bool IsValid,
    decimal DiscountAmount,
    string? Message);
