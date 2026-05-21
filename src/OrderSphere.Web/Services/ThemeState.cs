using MudBlazor;

namespace OrderSphere.Web.Services;

/// <summary>
/// Holds the active MudBlazor theme and dark-mode flag.
/// Components subscribe to OnChange for reactive rendering.
/// </summary>
public sealed class ThemeState
{
    private static readonly string[] FontFamily =
    [
        "Space Grotesk", "Inter", "-apple-system", "BlinkMacSystemFont", "Segoe UI", "sans-serif",
    ];

    private static readonly string[] MonoFamily =
    [
        "JetBrains Mono", "ui-monospace", "monospace",
    ];

    public MudTheme Theme { get; } = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary             = "#3A4DD1",
            PrimaryContrastText = "#FFFFFF",
            PrimaryDarken       = "#2B3BB8",
            PrimaryLighten      = "#6A7AE8",
            Secondary             = "#0D0D18",
            SecondaryContrastText = "#FFFFFF",
            Background     = "#FFFFFF",
            BackgroundGray = "#F1F2F8",
            Surface        = "#FFFFFF",
            TextPrimary   = "#0D0D18",
            TextSecondary = "#5A5A72",
            TextDisabled  = "rgba(13,13,24,0.30)",
            Success = "#2EA04B",
            Warning = "#FF9F0A",
            Error   = "#FF3B30",
            Info    = "#3A4DD1",
            Divider      = "#DCDDE6",
            DividerLight = "#E8E9F0",
            AppbarBackground = "rgba(255,255,255,0.92)",
            AppbarText       = "#0D0D18",
            DrawerBackground = "#FFFFFF",
            DrawerText       = "#0D0D18",
            DrawerIcon       = "#0D0D18",
            ActionDefault   = "#0D0D18",
        },
        PaletteDark = new PaletteDark
        {
            Primary             = "#3A4DD1",
            PrimaryContrastText = "#FFFFFF",
            PrimaryDarken       = "#2B3BB8",
            PrimaryLighten      = "#6A7AE8",
            Secondary             = "#EEEEF4",
            SecondaryContrastText = "#0D0E14",
            Background     = "#0D0E14",
            BackgroundGray = "#15161F",
            Surface        = "#1C1E2A",
            TextPrimary   = "#EEEEF4",
            TextSecondary = "rgba(238,238,244,0.70)",
            TextDisabled  = "rgba(238,238,244,0.28)",
            Success = "#3DB85F",
            Warning = "#FF9F0A",
            Error   = "#FF453A",
            Info    = "#3A4DD1",
            Divider      = "rgba(238,238,244,0.14)",
            DividerLight = "rgba(238,238,244,0.08)",
            AppbarBackground = "rgba(13,14,20,0.88)",
            AppbarText       = "#EEEEF4",
            DrawerBackground = "#1C1E2A",
            DrawerText       = "#EEEEF4",
            DrawerIcon       = "#EEEEF4",
            ActionDefault   = "#EEEEF4",
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "12px",
            AppbarHeight        = "72px",
        },
        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = FontFamily,
                FontSize   = "0.9375rem",
                FontWeight = "400",
                LineHeight = "1.6",
            },
            H1 = new H1Typography
            {
                FontFamily    = FontFamily,
                FontSize      = "3rem",
                FontWeight    = "600",
                LineHeight    = "1.05",
                LetterSpacing = "-0.035em",
            },
            H2 = new H2Typography
            {
                FontFamily    = FontFamily,
                FontSize      = "2.25rem",
                FontWeight    = "600",
                LineHeight    = "1.1",
                LetterSpacing = "-0.025em",
            },
            H3 = new H3Typography
            {
                FontFamily    = FontFamily,
                FontSize      = "1.875rem",
                FontWeight    = "600",
                LetterSpacing = "-0.015em",
            },
            H4 = new H4Typography
            {
                FontFamily    = FontFamily,
                FontSize      = "1.5rem",
                FontWeight    = "600",
                LetterSpacing = "-0.01em",
            },
            H5 = new H5Typography { FontFamily = FontFamily, FontSize = "1.25rem", FontWeight = "600" },
            H6 = new H6Typography { FontFamily = FontFamily, FontSize = "1.0625rem", FontWeight = "600" },
            Button = new ButtonTypography
            {
                FontSize      = "0.9375rem",
                FontWeight    = "600",
                TextTransform = "none",
            },
        },
    };

    public bool IsDarkMode { get; private set; }
    public event Action? OnChange;

    public void Toggle()
    {
        IsDarkMode = !IsDarkMode;
        OnChange?.Invoke();
    }
}
