namespace OrderSphere.Catalog.Application.DTOs;

public sealed record CategoryDto(
    Guid Id,
    string Name,
    string Description,
    int ProductCount);
