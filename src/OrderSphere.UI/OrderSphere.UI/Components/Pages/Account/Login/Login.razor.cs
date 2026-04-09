
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Identity;
using MudBlazor;
using OrderSphere.Domain.Entities;
using OrderSphere.Domain.Primitives;
using OrderSphere.UI.Configuration;
using OrderSphere.UI.Models.Auth;
using System.ComponentModel.DataAnnotations;
using System.Net;

namespace OrderSphere.UI.Components.Pages.Account.Login;

public partial class Login
{
    [SupplyParameterFromQuery]
    private string? ReturnUrl { get; set; }

    [CascadingParameter]
    private HttpContext HttpContext { get; set; } = default!;

    [SupplyParameterFromForm(Name = "loginForm")]
    private LoginModel Model { get; set; } = default!;

    private string? _errorMessage;

    protected override async Task OnInitializedAsync()
    {
        Model ??= new();
        if (!RendererInfo.IsInteractive && HttpContext is not null
            && HttpMethods.IsGet(HttpContext.Request.Method))
        {
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        }

        await Task.Run(() =>
        {
            Thread.Sleep(1000);
            Snackbar.Add("Email: " + Model.Email, Severity.Info);
        });
    }

    private async Task HandleLogin()
    {
        var result = await SignInManager.PasswordSignInAsync(
            Model.Email, Model.Password, Model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            NavManager.NavigateTo(ReturnUrl ?? "/");
        }
        else if (result.IsLockedOut)
            _errorMessage = "Dein Konto ist vorübergehend gesperrt.";
        else if (result.IsNotAllowed)
            _errorMessage = "Bitte bestätige zuerst deine E-Mail Adresse.";
        else
            _errorMessage = "E-Mail oder Passwort ist falsch.";
    }

    private sealed class LoginModel
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool RememberMe { get; set; }
    }
}