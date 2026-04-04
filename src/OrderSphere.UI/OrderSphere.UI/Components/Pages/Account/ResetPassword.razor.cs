using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.Azure.Amqp.Framing;
using MudBlazor;
using OrderSphere.Domain.Entities;
using System.Runtime.ConstrainedExecution;
using static Azure.Core.HttpHeader;

namespace OrderSphere.UI.Components.Pages.Account;

public partial class ResetPassword
{
    [SupplyParameterFromQuery] public string? Email { get; set; }
    [SupplyParameterFromQuery] public string? Token { get; set; }

    private readonly ResetPasswordModel _model = new();
    private List<string> _errors = [];
    private bool _isLoading;
    private bool _resetSucceeded;

    private bool _showPassword;
    private bool _showConfirmPassword;

    private InputType _passwordInputType => _showPassword ? InputType.Text : InputType.Password;
    private string _passwordIcon => _showPassword
        ? Icons.Material.Outlined.VisibilityOff
        : Icons.Material.Outlined.Visibility;

    private InputType _confirmPasswordInputType => _showConfirmPassword ? InputType.Text : InputType.Password;
    private string _confirmPasswordIcon => _showConfirmPassword
        ? Icons.Material.Outlined.VisibilityOff
        : Icons.Material.Outlined.Visibility;

    private void TogglePasswordVisibility() => _showPassword = !_showPassword;
    private void ToggleConfirmPasswordVisibility() => _showConfirmPassword = !_showConfirmPassword;

    protected override void OnInitialized()
    {
        // Token oder Email fehlen → zurück zur ForgotPassword Seite
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Token))
            NavManager.NavigateTo("/account/forgot-password");
    }

    private async Task HandleResetPassword()
    {
        _errors = [];
        _isLoading = true;

        if (_model.Password != _model.ConfirmPassword)
        {
            _errors.Add("Passwörter stimmen nicht überein.");
            _isLoading = false;
            return;
        }

        var user = await UserManager.FindByEmailAsync(Email!);

        if (user is null)
        {
            // Aus Sicherheitsgründen trotzdem Erfolg anzeigen
            _resetSucceeded = true;
            _isLoading = false;
            return;
        }

        var result = await UserManager.ResetPasswordAsync(user, Token!, _model.Password);

        if (result.Succeeded)
        {
            _resetSucceeded = true;
        }
        else
        {
            _errors = result.Errors.Select(e => e.Description).ToList();
        }

        _isLoading = false;
    }

    private sealed class ResetPasswordModel
    {
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}