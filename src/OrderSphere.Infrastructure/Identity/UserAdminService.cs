using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OrderSphere.Application.Abstraction;
using OrderSphere.Application.Models.Admin;
using OrderSphere.Domain.Entities;

namespace OrderSphere.Infrastructure.Identity;

public sealed class UserAdminService(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager
) : IUserAdminService
{
    public async Task<IReadOnlyList<AdminUserDto>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await userManager.Users
            .AsNoTracking()
            .OrderBy(u => u.Email)
            .ToListAsync(cancellationToken);

        var dtos = new List<AdminUserDto>(users.Count);
        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            dtos.Add(new AdminUserDto(
                user.Id,
                user.Email ?? string.Empty,
                user.FirstName,
                user.LastName,
                user.EmailConfirmed,
                roles.ToList()));
        }

        return dtos;
    }

    public async Task<IReadOnlyList<string>> GetAllRoleNamesAsync(CancellationToken cancellationToken = default)
    {
        return await roleManager.Roles
            .AsNoTracking()
            .Where(r => r.Name != null)
            .Select(r => r.Name!)
            .OrderBy(n => n)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> AssignRoleAsync(string userId, string roleName, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return false;

        if (await userManager.IsInRoleAsync(user, roleName)) return true;

        var result = await userManager.AddToRoleAsync(user, roleName);
        return result.Succeeded;
    }

    public async Task<bool> RemoveRoleAsync(string userId, string roleName, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return false;

        if (!await userManager.IsInRoleAsync(user, roleName)) return true;

        var result = await userManager.RemoveFromRoleAsync(user, roleName);
        return result.Succeeded;
    }
}
