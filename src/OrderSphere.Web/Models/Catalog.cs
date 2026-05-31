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
    bool IsActive);

public sealed record CategoryDto(
    Guid Id,
    string Name,
    string Description,
    int ProductCount);
