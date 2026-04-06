using Microsoft.AspNetCore.Components;
using MudBlazor;
using OrderSphere.Domain.Entities;
using OrderSphere.UI.Models.Auth;

namespace OrderSphere.UI.Components.Pages.Account;

public partial class Register
{
    private readonly RegisterModel _model = new();
    private List<string> _errors = [];
    private bool _isLoading;

    private bool _showPassword;
    private bool _showConfirmPassword;

    private InputType PasswordInputType => _showPassword ? InputType.Text : InputType.Password;
    private string PasswordIcon => _showPassword
        ? Icons.Material.Outlined.VisibilityOff
        : Icons.Material.Outlined.Visibility;

    private InputType ConfirmPasswordInputType => _showConfirmPassword ? InputType.Text : InputType.Password;
    private string ConfirmPasswordIcon => _showConfirmPassword
        ? Icons.Material.Outlined.VisibilityOff
        : Icons.Material.Outlined.Visibility;

    private void TogglePasswordVisibility() => _showPassword = !_showPassword;
    private void ToggleConfirmPasswordVisibility() => _showConfirmPassword = !_showConfirmPassword;

    private async Task HandleRegister()
    {
        _errors = [];
        _isLoading = true;

        if (_model.Password != _model.ConfirmPassword)
        {
            _errors.Add("Passwörter stimmen nicht überein.");
            _isLoading = false;
            return;
        }

        var user = new ApplicationUser
        {
            UserName = _model.Email,
            Email = _model.Email,
            FirstName = _model.FirstName,
            LastName = _model.LastName,
        };

        var result = await UserManager.CreateAsync(user, _model.Password);

        if (result.Succeeded)
        {

            var token = await UserManager.GenerateEmailConfirmationTokenAsync(user);

            var confirmationLink = $"{NavManager.BaseUri}confirm/email?userId={user.Id}&token={Uri.EscapeDataString(token)}";

            await EmailService.SendLinkAsync(user.Email, confirmationLink);
        }
        else
        {
            _errors = [.. result.Errors.Select(e => e.Description)];
        }

        _isLoading = false;
    }

    private sealed class RegisterModel
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}