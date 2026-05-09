namespace OrderSphere.Application.Models;

public sealed record CategoryDto(
    Guid Id,
    string Name,
    string Description,
    int ProductCount);
