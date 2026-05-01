using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Identity;
using OrderSphere.UI.Models.Auth;

namespace OrderSphere.UI.Components.Pages.Account.Login;

public partial class Login
{
    [SupplyParameterFromQuery]
    private string? ReturnUrl { get; set; }

    [CascadingParameter]
    private HttpContext HttpContext { get; set; } = default!;

    [SupplyParameterFromForm]
    public LoginModel Model { get; set; } = new();

    private string? _errorMessage;

    protected override async Task OnInitializedAsync()
    {
        Model ??= new();
        if (!RendererInfo.IsInteractive &&
            HttpContext is not null &&
            HttpMethods.IsGet(HttpContext.Request.Method))
        {
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        }
    }

    private async Task HandleLoginAsync(EditContext editContext)
    {
        var result = await SignInManager.PasswordSignInAsync(
            Model.Email, Model.Password, Model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            NavManager.NavigateTo(ReturnUrl ?? "/", true);
        }
        else if (result.IsLockedOut)
        {
            _errorMessage = "Dein Konto ist vorübergehend gesperrt.";
        }
        else if (result.IsNotAllowed)
        {
            _errorMessage = "Bitte bestätige zuerst deine E-Mail Adresse.";
        }
        else
        {
            _errorMessage = "E-Mail oder Passwort ist falsch.";
        }
    }

    public sealed class LoginModel
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool RememberMe { get; set; }
    }
}