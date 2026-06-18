namespace OrderSphere.Web.Models;

public sealed record ProductDto(
    Guid Id,
    string Name,
    string Slug,
    string Description,
    decimal Price,
    int Stock,
    Guid CategoryId,
    string CategoryName,
    string SKU,
    string? ImageUrl,
    bool IsActive,
    double AverageRating = 0,
    int ReviewCount = 0);

public sealed record CategoryDto(
    Guid Id,
    string Name,
    string Description,
    int ProductCount);

public sealed record ReviewDto(
    Guid Id,
    Guid ProductId,
    int Rating,
    string Title,
    string Body,
    bool IsVerifiedPurchase,
    string Status,
    DateTime CreatedAt);

public sealed record CreateReviewRequest(int Rating, string Title, string Body);
