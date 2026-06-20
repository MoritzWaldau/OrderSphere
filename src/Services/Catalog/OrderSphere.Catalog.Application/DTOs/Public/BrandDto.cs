namespace OrderSphere.Catalog.Application.DTOs.Public;

public sealed record BrandDto(
    Guid Id,
    string Name,
    string Slug,
    string Description,
    string? LogoUrl,
    int ProductCount);
