using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using OrderSphere.Application.Abstraction;
using OrderSphere.Domain.Entities;

namespace OrderSphere.UI.Components.Pages.Account;

public partial class ForgotPassword
{
    private string _email = string.Empty;
    private string? _errorMessage;
    private bool _isLoading;
    private bool _emailSent;

    private async Task HandleForgotPassword()
    {
        _errorMessage = null;
        _isLoading = true;

        if (string.IsNullOrWhiteSpace(_email))
        {
            _errorMessage = "Bitte gib deine E-Mail Adresse ein.";
            _isLoading = false;
            return;
        }

        var user = await UserManager.FindByEmailAsync(_email);

        // Aus Sicherheitsgründen immer Erfolg anzeigen
        // auch wenn der User nicht existiert
        if (user is not null)
        {
            var token = await UserManager.GeneratePasswordResetTokenAsync(user);
            var resetLink = NavManager.ToAbsoluteUri(
                $"/account/reset-password?email={Uri.EscapeDataString(_email)}&token={Uri.EscapeDataString(token)}"
            ).ToString();

            await EmailService.SendPasswordResetEmailAsync(_email, resetLink);
        }

        _emailSent = true;
        _isLoading = false;
    }
}