using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using OrderSphere.Application.Features.Account.GetDarkModePreference;
using OrderSphere.Application.Features.Account.SaveDarkModePreference;
using OrderSphere.UI.Services.Auth;

namespace OrderSphere.UI.Services.Theme;

public sealed class ThemeService(
    ICurrentUserService currentUserService,
    ISender sender,
    IJSRuntime js
) : IThemeService, IAsyncDisposable
{
    private IJSObjectReference? _module;
    private bool _initialized;

    public bool IsDarkMode { get; private set; }
    public event Action? OnChange;

    public async Task InitializeAsync()
    {
        if (_initialized) return;
        _initialized = true;

        _module = await js.InvokeAsync<IJSObjectReference>("import", "./js/theme.js");

        var userId = await currentUserService.GetUserIdAsync();
        if (userId is not null)
        {
            var result = await sender.Send(new GetDarkModePreferenceQuery(userId));
            if (result.IsSuccess)
            {
                IsDarkMode = result.Value;
                await _module.InvokeVoidAsync("setThemePreference", IsDarkMode);
                return;
            }
        }

        IsDarkMode = await _module.InvokeAsync<bool>("getThemePreference");
    }

    public async Task ToggleAsync()
    {
        IsDarkMode = !IsDarkMode;

        if (_module is not null)
            await _module.InvokeVoidAsync("setThemePreference", IsDarkMode);

        var userId = await currentUserService.GetUserIdAsync();
        if (userId is not null)
            await sender.Send(new SaveDarkModePreferenceCommand(userId, IsDarkMode));

        OnChange?.Invoke();
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
            await _module.DisposeAsync();
    }
}
