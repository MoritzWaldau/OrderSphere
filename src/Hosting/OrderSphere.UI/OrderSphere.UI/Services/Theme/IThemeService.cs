namespace OrderSphere.UI.Services.Theme;

public interface IThemeService
{
    bool IsDarkMode { get; }
    event Action? OnChange;
    Task InitializeAsync();
    Task ToggleAsync();
}
