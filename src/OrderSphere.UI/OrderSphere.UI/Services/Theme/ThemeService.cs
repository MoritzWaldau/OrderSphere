using Microsoft.JSInterop;

namespace OrderSphere.UI.Services.Theme;

public sealed class ThemeService(IJSRuntime js) : IThemeService, IAsyncDisposable
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
        IsDarkMode = await _module.InvokeAsync<bool>("getThemePreference");
    }

    public async Task ToggleAsync()
    {
        IsDarkMode = !IsDarkMode;

        if (_module is not null)
            await _module.InvokeVoidAsync("setThemePreference", IsDarkMode);

        OnChange?.Invoke();
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
            await _module.DisposeAsync();
    }
}
