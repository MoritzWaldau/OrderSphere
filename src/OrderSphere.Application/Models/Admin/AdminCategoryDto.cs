namespace OrderSphere.Application.Models.Admin;

public sealed record AdminCategoryDto(
    Guid Id,
    string Name,
    string Description,
    bool IsActive,
    int ProductCount,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record AdminCategoryInput(string Name, string Description);
