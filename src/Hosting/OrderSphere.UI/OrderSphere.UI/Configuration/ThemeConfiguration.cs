using MudBlazor;

namespace OrderSphere.UI.Configuration;

public static class ThemeConfiguration
{
    private static readonly string[] FontFamily =
    [
        "Space Grotesk",
        "Inter",
        "-apple-system",
        "BlinkMacSystemFont",
        "Segoe UI",
        "sans-serif",
    ];

    private static readonly string[] MonoFamily =
    [
        "JetBrains Mono",
        "ui-monospace",
        "monospace",
    ];

    public static MudTheme ApplicationTheme() => new()
    {
        PaletteLight = new PaletteLight
        {
            // ── Brand ────────────────────────────────────────────
            Primary             = "#3A4DD1",   // Indigo
            PrimaryContrastText = "#FFFFFF",
            PrimaryDarken       = "#2B3BB8",
            PrimaryLighten      = "#6A7AE8",

            Secondary             = "#0D0D18",
            SecondaryContrastText = "#FFFFFF",
            SecondaryDarken       = "#000000",
            SecondaryLighten      = "#3D3D4A",

            Tertiary             = "#2EA04B",
            TertiaryContrastText = "#FFFFFF",

            // ── Backgrounds ──────────────────────────────────────
            Background     = "#FFFFFF",
            BackgroundGray = "#F1F2F8",
            Surface        = "#FFFFFF",

            // ── Text ─────────────────────────────────────────────
            TextPrimary   = "#0D0D18",
            TextSecondary = "#5A5A72",
            TextDisabled  = "rgba(13,13,24,0.30)",

            // ── States ───────────────────────────────────────────
            Success = "#2EA04B",
            Warning = "#FF9F0A",
            Error   = "#FF3B30",
            Info    = "#3A4DD1",

            SuccessContrastText = "#FFFFFF",
            WarningContrastText = "#FFFFFF",
            ErrorContrastText   = "#FFFFFF",
            InfoContrastText    = "#FFFFFF",

            // ── UI chrome ────────────────────────────────────────
            Divider      = "#DCDDE6",
            DividerLight = "#E8E9F0",

            AppbarBackground = "rgba(255,255,255,0.92)",
            AppbarText       = "#0D0D18",

            DrawerBackground = "#FFFFFF",
            DrawerText       = "#0D0D18",
            DrawerIcon       = "#0D0D18",

            // ── Tables / actions ─────────────────────────────────
            LinesDefault    = "#E8E8EF",
            TableLines      = "#E8E8EF",
            TableStriped    = "rgba(0,0,0,0.02)",
            TableHover      = "rgba(0,0,0,0.04)",
            ActionDefault   = "#0D0D18",
            ActionDisabled  = "rgba(13,13,24,0.30)",
            ActionDisabledBackground = "rgba(13,13,24,0.08)",

            HoverOpacity  = 0.06,
            RippleOpacity = 0.08,

            OverlayDark  = "rgba(0,0,0,0.5)",
            OverlayLight = "rgba(255,255,255,0.7)",
        },

        PaletteDark = new PaletteDark
        {
            // ── Brand ────────────────────────────────────────────
            Primary             = "#3A4DD1",   // same indigo as light mode for cross-mode consistency
            PrimaryContrastText = "#FFFFFF",
            PrimaryDarken       = "#2B3BB8",
            PrimaryLighten      = "#6A7AE8",

            Secondary             = "#EEEEF4",
            SecondaryContrastText = "#0D0E14",
            SecondaryDarken       = "#FFFFFF",
            SecondaryLighten      = "#C0C0CC",

            Tertiary             = "#3DB85F",
            TertiaryContrastText = "#FFFFFF",

            // ── Backgrounds ──────────────────────────────────────
            Background     = "#0D0E14",
            BackgroundGray = "#15161F",
            Surface        = "#1C1E2A",

            // ── Text ─────────────────────────────────────────────
            TextPrimary   = "#EEEEF4",
            TextSecondary = "rgba(238,238,244,0.70)",
            TextDisabled  = "rgba(238,238,244,0.28)",

            // ── States ───────────────────────────────────────────
            Success = "#3DB85F",
            Warning = "#FF9F0A",
            Error   = "#FF453A",
            Info    = "#3A4DD1",

            SuccessContrastText = "#FFFFFF",
            WarningContrastText = "#FFFFFF",
            ErrorContrastText   = "#FFFFFF",
            InfoContrastText    = "#FFFFFF",

            // ── UI chrome ────────────────────────────────────────
            Divider      = "rgba(238,238,244,0.14)",
            DividerLight = "rgba(238,238,244,0.08)",

            AppbarBackground = "rgba(13,14,20,0.88)",
            AppbarText       = "#EEEEF4",

            DrawerBackground = "#1C1E2A",
            DrawerText       = "#EEEEF4",
            DrawerIcon       = "#EEEEF4",

            // ── Tables / actions ─────────────────────────────────
            LinesDefault    = "rgba(238,238,244,0.10)",
            TableLines      = "rgba(238,238,244,0.10)",
            TableStriped    = "rgba(238,238,244,0.03)",
            TableHover      = "rgba(238,238,244,0.06)",
            ActionDefault   = "#EEEEF4",
            ActionDisabled  = "rgba(238,238,244,0.28)",
            ActionDisabledBackground = "rgba(238,238,244,0.08)",

            HoverOpacity  = 0.08,
            RippleOpacity = 0.10,

            OverlayDark  = "rgba(0,0,0,0.65)",
            OverlayLight = "rgba(238,238,244,0.08)",
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
                FontFamily  = FontFamily,
                FontSize    = "0.9375rem",
                FontWeight  = "400",
                LineHeight  = "1.6",
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
                LineHeight    = "1.15",
                LetterSpacing = "-0.015em",
            },
            H4 = new H4Typography
            {
                FontFamily    = FontFamily,
                FontSize      = "1.5rem",
                FontWeight    = "600",
                LineHeight    = "1.2",
                LetterSpacing = "-0.01em",
            },
            H5 = new H5Typography
            {
                FontFamily = FontFamily,
                FontSize   = "1.25rem",
                FontWeight = "600",
                LineHeight = "1.3",
            },
            H6 = new H6Typography
            {
                FontFamily = FontFamily,
                FontSize   = "1.0625rem",
                FontWeight = "600",
                LineHeight = "1.4",
            },
            Body1 = new Body1Typography
            {
                FontSize   = "0.9375rem",
                FontWeight = "400",
                LineHeight = "1.6",
            },
            Body2 = new Body2Typography
            {
                FontSize   = "0.8125rem",
                FontWeight = "400",
                LineHeight = "1.5",
            },
            Subtitle1 = new Subtitle1Typography
            {
                FontSize   = "1rem",
                FontWeight = "500",
                LineHeight = "1.5",
            },
            Subtitle2 = new Subtitle2Typography
            {
                FontSize   = "0.875rem",
                FontWeight = "500",
                LineHeight = "1.5",
            },
            Caption = new CaptionTypography
            {
                FontSize      = "0.75rem",
                FontWeight    = "400",
                LineHeight    = "1.4",
                LetterSpacing = "0.01em",
            },
            Button = new ButtonTypography
            {
                FontSize      = "0.9375rem",
                FontWeight    = "600",
                TextTransform = "none",
                LetterSpacing = "-0.005em",
            },
            Overline = new OverlineTypography
            {
                FontFamily    = MonoFamily,
                FontSize      = "0.6875rem",
                FontWeight    = "500",
                LetterSpacing = "0.08em",
                LineHeight    = "1.4",
            },
        },
    };
}
