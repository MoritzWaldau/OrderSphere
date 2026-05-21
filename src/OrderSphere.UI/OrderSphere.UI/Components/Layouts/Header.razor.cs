using Microsoft.AspNetCore.Components;

namespace OrderSphere.UI.Components.Layouts;

public partial class Header
{
    [Parameter] public bool IsCartOpen { get; set; }
    [Parameter] public EventCallback OnCartToggle { get; set; }
    [Parameter] public int CartItemCount { get; set; }

    [Inject] private IConfiguration Configuration { get; set; } = default!;

    private bool _drawerOpen = false;
    private void ToggleDrawer() => _drawerOpen = !_drawerOpen;

    private string KeycloakAccountUrl =>
        $"{Configuration["Keycloak:Authority"]?.TrimEnd('/')}/account";

    private async Task HandleLogout()
    {
        NavManager.NavigateTo("/logout", true);
    }
}