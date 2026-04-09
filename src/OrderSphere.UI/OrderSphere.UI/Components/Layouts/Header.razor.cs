using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using OrderSphere.Domain.Entities;

namespace OrderSphere.UI.Components.Layouts;

public partial class Header
{
    [Parameter] public bool IsCartOpen { get; set; }
    [Parameter] public EventCallback OnCartToggle { get; set; }
    [Parameter] public int CartItemCount { get; set; }

    private bool _drawerOpen = false;
    private void ToggleDrawer() => _drawerOpen = !_drawerOpen;

    private async Task HandleLogout()
    {
        NavManager.NavigateTo("/account/logout", true);
    }
}