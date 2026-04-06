using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using OrderSphere.Domain.Entities;

namespace OrderSphere.UI.Components.Pages.Account;

public partial class ConfirmEmail
{
    [SupplyParameterFromQuery]
    public string? UserId { get; set; }

    [SupplyParameterFromQuery]
    public string? Token { get; set; }

    private string Message = "E-Mail wird bestätigt...";
    private bool IsSuccess = false;

    protected override async Task OnInitializedAsync()
    {
        if (string.IsNullOrEmpty(UserId) || string.IsNullOrEmpty(Token))
        {
            Message = "Ungültige Anfrage.";
            return;
        }

        var user = await UserManager.FindByIdAsync(UserId);
        if (user == null)
        {
            Message = "Benutzer nicht gefunden.";
            return;
        }

        var result = await UserManager.ConfirmEmailAsync(user, Token);

        if (result.Succeeded)
        {
            Message = "E-Mail erfolgreich bestätigt! Du kannst dich jetzt anmelden.";
            IsSuccess = true;
        }
        else
        {
            Message = "Bestätigung fehlgeschlagen. Der Link ist möglicherweise abgelaufen.";
        }
    }
}
