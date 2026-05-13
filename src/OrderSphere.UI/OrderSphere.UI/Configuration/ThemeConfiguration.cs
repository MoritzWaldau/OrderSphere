using MudBlazor;

namespace OrderSphere.UI.Configuration;

public static class ThemeConfiguration
{
    public static MudTheme ApplicationTheme()
    {
        return new MudTheme()
        {
            PaletteLight = new PaletteLight
            {
                // 🌟 PRIMARY BRAND
                Primary = "#0071E3",                    // Apple Blau
                PrimaryContrastText = "#FFFFFF",
                PrimaryDarken = "#0051A8",
                PrimaryLighten = "#3395FF",

                Secondary = "#1D1D1F",                  // Apple Dunkelgrau (fast schwarz)
                SecondaryContrastText = "#FFFFFF",
                SecondaryDarken = "#000000",
                SecondaryLighten = "#3D3D3F",

                Tertiary = "#06C167",                   // Amazon Fresh Grün
                TertiaryContrastText = "#FFFFFF",

                // 🎨 BACKGROUND
                Background = "#FFFFFF",                 // Apple: reines Weiß
                BackgroundGray = "#F5F5F7",            // Apple: sehr helles Grau für Sektionen
                Surface = "#FFFFFF",

                // 📝 TEXT
                TextPrimary = "#1D1D1F",               // Apple: fast schwarz
                TextSecondary = "#6E6E73",             // Apple: mittelgrau
                TextDisabled = "rgba(29,29,31,0.30)",

                // ✅ STATES
                Success = "#06C167",
                Warning = "#FF9F0A",                   // Apple Warning Orange
                Error = "#FF3B30",                     // Apple Rot
                Info = "#0071E3",

                SuccessContrastText = "#FFFFFF",
                WarningContrastText = "#FFFFFF",
                ErrorContrastText = "#FFFFFF",
                InfoContrastText = "#FFFFFF",

                // 📏 UI
                Divider = "#D2D2D7",                   // Apple: helles Grau
                DividerLight = "#E8E8ED",

                // 🧊 EFFECTS
                HoverOpacity = 0.06,
                RippleOpacity = 0.08,

                // 🧱 LAYOUT
                AppbarBackground = "rgba(255,255,255,0.85)",  // Apple: Frosted Glass
                AppbarText = "#1D1D1F",

                DrawerBackground = "#FFFFFF",
                DrawerText = "#1D1D1F",
                DrawerIcon = "#1D1D1F",

                // 🃏 CARDS / PAPER
                LinesDefault = "#D2D2D7",
                TableLines = "#D2D2D7",
                TableStriped = "rgba(0,0,0,0.02)",
                TableHover = "rgba(0,0,0,0.04)",

                // 🔲 ACTION
                ActionDefault = "#1D1D1F",
                ActionDisabled = "rgba(29,29,31,0.30)",
                ActionDisabledBackground = "rgba(29,29,31,0.08)",

                // Overlay
                OverlayDark = "rgba(0,0,0,0.5)",
                OverlayLight = "rgba(255,255,255,0.7)",
            },

            LayoutProperties = new LayoutProperties
            {
                DefaultBorderRadius = "12px",          // Apple: sanfte Rundung
                AppbarHeight = "72px",
            },

            Typography = new Typography
            {
                Default = new DefaultTypography
                {
                    FontFamily = new[]
                    {
                        "Inter",
                        "-apple-system",
                        "BlinkMacSystemFont",
                        "SF Pro Display",
                        "Segoe UI",
                        "Roboto",
                        "Arial",
                        "sans-serif"
                    },
                    FontSize = "0.9375rem",            // 15px – Apple Standard
                    FontWeight = "400",
                    LineHeight = "1.6"
                },

                H1 = new H1Typography
                {
                    FontSize = "3rem",                 // 48px
                    FontWeight = "700",
                    LineHeight = "1.1",
                    LetterSpacing = "-0.02em"          // Apple: enges Letter Spacing bei Headlines
                },
                H2 = new H2Typography
                {
                    FontSize = "2.25rem",              // 36px
                    FontWeight = "700",
                    LineHeight = "1.15",
                    LetterSpacing = "-0.015em"
                },
                H3 = new H3Typography
                {
                    FontSize = "1.875rem",             // 30px
                    FontWeight = "600",
                    LineHeight = "1.2",
                    LetterSpacing = "-0.01em"
                },
                H4 = new H4Typography
                {
                    FontSize = "1.5rem",               // 24px
                    FontWeight = "600",
                    LineHeight = "1.25",
                    LetterSpacing = "-0.01em"
                },
                H5 = new H5Typography
                {
                    FontSize = "1.25rem",              // 20px
                    FontWeight = "600",
                    LineHeight = "1.3"
                },
                H6 = new H6Typography
                {
                    FontSize = "1.0625rem",            // 17px – Apple body large
                    FontWeight = "600",
                    LineHeight = "1.4"
                },

                Body1 = new Body1Typography
                {
                    FontSize = "0.9375rem",            // 15px
                    FontWeight = "400",
                    LineHeight = "1.6"
                },
                Body2 = new Body2Typography
                {
                    FontSize = "0.8125rem",            // 13px
                    FontWeight = "400",
                    LineHeight = "1.5"
                },

                Subtitle1 = new Subtitle1Typography
                {
                    FontSize = "1rem",
                    FontWeight = "500",
                    LineHeight = "1.5"
                },
                Subtitle2 = new Subtitle2Typography
                {
                    FontSize = "0.875rem",
                    FontWeight = "500",
                    LineHeight = "1.5"
                },

                Caption = new CaptionTypography
                {
                    FontSize = "0.75rem",              // 12px
                    FontWeight = "400",
                    LineHeight = "1.4",
                    LetterSpacing = "0.01em"
                },

                Button = new ButtonTypography
                {
                    FontSize = "0.9375rem",
                    FontWeight = "500",
                    TextTransform = "none",            // Apple: kein Uppercase
                    LetterSpacing = "0"
                },

                Overline = new OverlineTypography
                {
                    FontSize = "0.6875rem",            // 11px
                    FontWeight = "500",
                    LetterSpacing = "0.08em",
                    LineHeight = "1.4"
                }
            }
        };
    }
}