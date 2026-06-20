namespace OrderSphere.Catalog.Application.DTOs.Public;

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
    double AverageRating,
    int ReviewCount,
    Guid? BrandId = null,
    string? BrandName = null);
