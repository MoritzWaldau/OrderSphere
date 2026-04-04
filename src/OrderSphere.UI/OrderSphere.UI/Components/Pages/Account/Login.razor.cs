
using MudBlazor;

namespace OrderSphere.UI.Components.Pages.Account;

public partial class Login
{
    private readonly LoginModel _model = new();
    private string? _errorMessage;
    private bool _isLoading;

    private bool _showPassword;
    private InputType PasswordInputType => _showPassword ? InputType.Text : InputType.Password;
    private string PasswordIcon => _showPassword
        ? Icons.Material.Outlined.VisibilityOff
        : Icons.Material.Outlined.Visibility;

    private void TogglePasswordVisibility() => _showPassword = !_showPassword;

    private async Task HandleLogin()
    {
        _errorMessage = null;
        _isLoading = true;

        var result = await SignInManager.PasswordSignInAsync(
            _model.Email,
            _model.Password,
            _model.RememberMe,
            lockoutOnFailure: true
        );

        if (result.Succeeded)
        {
            NavManager.NavigateTo("/", forceLoad: true);
        }
        else if (result.IsLockedOut)
        {
            _errorMessage = "Dein Konto ist vorübergehend gesperrt. Bitte versuche es später erneut.";
        }
        else if (result.IsNotAllowed)
        {
            _errorMessage = "Bitte bestätige zuerst deine E-Mail Adresse.";
        }
        else
        {
            _errorMessage = "E-Mail oder Passwort ist falsch.";
        }

        _isLoading = false;
    }

    private sealed class LoginModel
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool RememberMe { get; set; }
    }
}