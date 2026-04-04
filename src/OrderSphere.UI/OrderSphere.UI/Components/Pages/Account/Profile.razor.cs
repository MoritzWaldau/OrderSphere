using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using MudBlazor;
using OrderSphere.Domain.Entities;

namespace OrderSphere.UI.Components.Pages.Account;

public partial class Profile
{
    private ApplicationUser? _user;
    private string _initials = string.Empty;

    // Name
    private readonly NameModel _nameModel = new();
    private string? _nameError;
    private bool _nameSuccess;
    private bool _nameLoading;

    // Email
    private readonly EmailModel _emailModel = new();
    private string? _emailError;
    private bool _emailSuccess;
    private bool _emailLoading;

    // Password
    private readonly PasswordModel _passwordModel = new();
    private List<string> _passwordErrors = [];
    private bool _passwordSuccess;
    private bool _passwordLoading;
    private bool _showCurrentPassword;
    private bool _showNewPassword;
    private bool _showConfirmPassword;

    private InputType _currentPasswordType => _showCurrentPassword ? InputType.Text : InputType.Password;
    private string _currentPasswordIcon => _showCurrentPassword ? Icons.Material.Outlined.VisibilityOff : Icons.Material.Outlined.Visibility;
    private InputType _newPasswordType => _showNewPassword ? InputType.Text : InputType.Password;
    private string _newPasswordIcon => _showNewPassword ? Icons.Material.Outlined.VisibilityOff : Icons.Material.Outlined.Visibility;
    private InputType _confirmPasswordType => _showConfirmPassword ? InputType.Text : InputType.Password;
    private string _confirmPasswordIcon => _showConfirmPassword ? Icons.Material.Outlined.VisibilityOff : Icons.Material.Outlined.Visibility;

    // Delete
    private bool _showDeleteConfirm;
    private string _deletePassword = string.Empty;
    private bool _deleteLoading;

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        _user = await UserManager.GetUserAsync(authState.User);

        if (_user is null)
        {
            NavManager.NavigateTo("/account/login");
            return;
        }

        _nameModel.FirstName = _user.FirstName;
        _nameModel.LastName = _user.LastName;
        _emailModel.NewEmail = _user.Email ?? string.Empty;
        _initials = $"{_user.FirstName[0]}{_user.LastName[0]}".ToUpper();
    }

    private async Task HandleNameChange()
    {
        _nameError = null;
        _nameSuccess = false;
        _nameLoading = true;

        _user!.FirstName = _nameModel.FirstName;
        _user!.LastName = _nameModel.LastName;

        var result = await UserManager.UpdateAsync(_user!);

        if (result.Succeeded)
        {
            _nameSuccess = true;
            _initials = $"{_user.FirstName[0]}{_user.LastName[0]}".ToUpper();
        }
        else
        {
            _nameError = result.Errors.First().Description;
        }

        _nameLoading = false;
    }

    private async Task HandleEmailChange()
    {
        _emailError = null;
        _emailSuccess = false;
        _emailLoading = true;

        if (_emailModel.NewEmail == _user!.Email)
        {
            _emailError = "Das ist bereits deine aktuelle E-Mail Adresse.";
            _emailLoading = false;
            return;
        }

        var token = await UserManager.GenerateChangeEmailTokenAsync(_user!, _emailModel.NewEmail);
        var result = await UserManager.ChangeEmailAsync(_user!, _emailModel.NewEmail, token);

        if (result.Succeeded)
        {
            await UserManager.SetUserNameAsync(_user!, _emailModel.NewEmail);
            _emailSuccess = true;
        }
        else
        {
            _emailError = result.Errors.First().Description;
        }

        _emailLoading = false;
    }

    private async Task HandlePasswordChange()
    {
        _passwordErrors = [];
        _passwordSuccess = false;
        _passwordLoading = true;

        if (_passwordModel.NewPassword != _passwordModel.ConfirmPassword)
        {
            _passwordErrors.Add("Passwörter stimmen nicht überein.");
            _passwordLoading = false;
            return;
        }

        var result = await UserManager.ChangePasswordAsync(
            _user!,
            _passwordModel.CurrentPassword,
            _passwordModel.NewPassword
        );

        if (result.Succeeded)
        {
            _passwordSuccess = true;
            _passwordModel.CurrentPassword = string.Empty;
            _passwordModel.NewPassword = string.Empty;
            _passwordModel.ConfirmPassword = string.Empty;
        }
        else
        {
            _passwordErrors = result.Errors.Select(e => e.Description).ToList();
        }

        _passwordLoading = false;
    }

    private async Task HandleDeleteAccount()
    {
        _deleteLoading = true;

        var checkPassword = await UserManager.CheckPasswordAsync(_user!, _deletePassword);

        if (!checkPassword)
        {
            _deleteLoading = false;
            return;
        }

        await SignInManager.SignOutAsync();
        await UserManager.DeleteAsync(_user!);
        NavManager.NavigateTo("/", forceLoad: true);
    }

    private sealed class NameModel
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
    }

    private sealed class EmailModel
    {
        public string NewEmail { get; set; } = string.Empty;
    }

    private sealed class PasswordModel
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}