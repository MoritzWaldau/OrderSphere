namespace OrderSphere.Catalog.Application.DTOs.Admin;

public sealed record AdminBrandDto(
    Guid Id,
    string Name,
    string Slug,
    string Description,
    string? LogoUrl,
    bool IsActive,
    int ProductCount,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record AdminBrandInput(string Name, string Description, string? LogoUrl = null, bool IsActive = true);
