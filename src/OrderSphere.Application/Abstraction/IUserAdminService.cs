using OrderSphere.Application.Models.Admin;

namespace OrderSphere.Application.Abstraction;

public interface IUserAdminService
{
    Task<IReadOnlyList<AdminUserDto>> GetAllUsersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetAllRoleNamesAsync(CancellationToken cancellationToken = default);
    Task<bool> AssignRoleAsync(string userId, string roleName, CancellationToken cancellationToken = default);
    Task<bool> RemoveRoleAsync(string userId, string roleName, CancellationToken cancellationToken = default);
}
