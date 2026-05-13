namespace OrderSphere.UI.Services;

public interface IThemeService
{
    bool IsDarkMode { get; }
    event Action? OnChange;
    Task InitializeAsync(bool? prefersDark);
    Task ToggleAsync();
    Task SetAsync(bool isDarkMode);
}
