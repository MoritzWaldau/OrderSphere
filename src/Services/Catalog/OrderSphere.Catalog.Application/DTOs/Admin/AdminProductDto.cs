namespace OrderSphere.Catalog.Application.DTOs.Admin;

public sealed record AdminProductDto(
    Guid Id,
    string Name,
    string Slug,
    string Description,
    decimal Price,
    int Stock,
    Guid CategoryId,
    string CategoryName,
    string SKU,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record AdminProductInput(
    string Name,
    string Description,
    decimal Price,
    int Stock,
    Guid CategoryId,
    string SKU,
    bool IsActive = true,
    string? ImageUrl = null);
