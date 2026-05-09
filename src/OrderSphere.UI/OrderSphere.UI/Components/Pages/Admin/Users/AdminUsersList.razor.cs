using Microsoft.AspNetCore.Components;
using MudBlazor;
using OrderSphere.Application.Abstraction;
using OrderSphere.Application.Models.Admin;
using OrderSphere.UI.Configuration;

namespace OrderSphere.UI.Components.Pages.Admin.Users;

public partial class AdminUsersList : OrderSphereComponentBase
{
    [Inject] public required IUserAdminService UserAdminService { get; set; }

    private IReadOnlyList<AdminUserDto> _users = Array.Empty<AdminUserDto>();
    private IReadOnlyList<string> _availableRoles = Array.Empty<string>();

    protected override async Task LoadDataAsync()
    {
        _users = await UserAdminService.GetAllUsersAsync();
        _availableRoles = await UserAdminService.GetAllRoleNamesAsync();
    }

    private async Task AssignRoleAsync(string userId, string roleName)
    {
        var ok = await UserAdminService.AssignRoleAsync(userId, roleName);
        if (ok)
        {
            Snackbar.Add($"Rolle '{roleName}' zugewiesen.", Severity.Success);
            await LoadDataAsync();
            StateHasChanged();
        }
        else
        {
            Snackbar.Add("Rolle konnte nicht zugewiesen werden.", Severity.Error);
        }
    }

    private async Task RemoveRoleAsync(string userId, string roleName)
    {
        var ok = await UserAdminService.RemoveRoleAsync(userId, roleName);
        if (ok)
        {
            Snackbar.Add($"Rolle '{roleName}' entfernt.", Severity.Success);
            await LoadDataAsync();
            StateHasChanged();
        }
        else
        {
            Snackbar.Add("Rolle konnte nicht entfernt werden.", Severity.Error);
        }
    }
}
