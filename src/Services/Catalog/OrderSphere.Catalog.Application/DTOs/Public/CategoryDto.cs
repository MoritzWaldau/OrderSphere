namespace OrderSphere.Catalog.Application.DTOs.Public;

public sealed record CategoryDto(
    Guid Id,
    string Name,
    string Description,
    int ProductCount);
