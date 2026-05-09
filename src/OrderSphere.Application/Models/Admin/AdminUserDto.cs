namespace OrderSphere.Application.Models.Admin;

public sealed record AdminUserDto(
    string Id,
    string Email,
    string FirstName,
    string LastName,
    bool EmailConfirmed,
    IReadOnlyList<string> Roles);
