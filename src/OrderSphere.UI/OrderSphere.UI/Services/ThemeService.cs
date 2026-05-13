using Microsoft.JSInterop;

namespace OrderSphere.UI.Services;

public sealed class ThemeService(IJSRuntime js) : IThemeService
{
    private const string StorageKey = "ordersphere.theme";

    public bool IsDarkMode { get; private set; }

    public event Action? OnChange;

    public async Task InitializeAsync(bool? prefersDark)
    {
        var stored = await TryReadStorageAsync();

        IsDarkMode = stored ?? prefersDark ?? false;
        OnChange?.Invoke();
    }

    public Task ToggleAsync() => SetAsync(!IsDarkMode);

    public async Task SetAsync(bool isDarkMode)
    {
        if (IsDarkMode == isDarkMode) return;

        IsDarkMode = isDarkMode;

        try
        {
            await js.InvokeVoidAsync("localStorage.setItem", StorageKey, isDarkMode ? "dark" : "light");
        }
        catch (JSException)
        {
            // Storage may be blocked (private mode); ignore.
        }
        catch (InvalidOperationException)
        {
            // Pre-render path — storage not available yet.
        }

        OnChange?.Invoke();
    }

    private async Task<bool?> TryReadStorageAsync()
    {
        try
        {
            var value = await js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
            return value switch
            {
                "dark" => true,
                "light" => false,
                _ => null
            };
        }
        catch (JSException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }
}
