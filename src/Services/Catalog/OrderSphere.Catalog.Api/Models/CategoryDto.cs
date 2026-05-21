namespace OrderSphere.Catalog.Api.Models;

public sealed record CategoryDto(
    Guid Id,
    string Name,
    string Description,
    int ProductCount);
