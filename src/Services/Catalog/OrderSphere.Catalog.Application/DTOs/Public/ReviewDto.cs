namespace OrderSphere.Catalog.Application.DTOs.Public;

public sealed record ReviewDto(
    Guid Id,
    Guid ProductId,
    int Rating,
    string Title,
    string Body,
    bool IsVerifiedPurchase,
    string Status,
    DateTime CreatedAt);
