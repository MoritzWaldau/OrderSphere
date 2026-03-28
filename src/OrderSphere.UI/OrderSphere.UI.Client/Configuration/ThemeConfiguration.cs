using MudBlazor;

namespace OrderSphere.UI.Client.Configuration;

public static class ThemeConfiguration
{
    public static MudTheme ApplicationTheme()
    {
        return new MudTheme()
        {
            PaletteLight = new PaletteLight
            {
                // 🌟 PRIMARY BRAND
                Primary = "#6366F1",           // modern indigo (sehr beliebt aktuell)
                PrimaryContrastText = "#FFFFFF",

                Secondary = "#64748B",         // slate gray

                // 🎨 BACKGROUND
                Background = "#F8FAFC",        // sehr helles grau (moderner als #F5F5F5)
                Surface = "#FFFFFF",

                // 📝 TEXT
                TextPrimary = "#0F172A",       // fast schwarz (besser lesbar)
                TextSecondary = "#475569",
                TextDisabled = "rgba(15,23,42,0.38)",

                // ✅ STATES
                Success = "#22C55E",
                Warning = "#F59E0B",
                Error = "#EF4444",
                Info = "#3B82F6",

                SuccessContrastText = "#FFFFFF",
                WarningContrastText = "#FFFFFF",
                ErrorContrastText = "#FFFFFF",
                InfoContrastText = "#FFFFFF",

                // 📏 UI
                Divider = "#E2E8F0",

                // 🧊 EFFECTS
                HoverOpacity = 0.04,
                RippleOpacity = 0.06,

                // 🧱 LAYOUT
                AppbarBackground = "#FFFFFF",
                AppbarText = "#0F172A",

                DrawerBackground = "#FFFFFF",
                DrawerText = "#0F172A",
            },

            PaletteDark = new PaletteDark
            {
                // 🌟 PRIMARY
                Primary = "#818CF8",           // helleres indigo für dark mode
                PrimaryContrastText = "#0F172A",

                Secondary = "#94A3B8",

                // 🎨 BACKGROUND
                Background = "#0B1120",        // tiefes blau/schwarz (moderner als #121212)
                Surface = "#111827",

                // 📝 TEXT
                TextPrimary = "#E2E8F0",
                TextSecondary = "#94A3B8",
                TextDisabled = "rgba(226,232,240,0.38)",

                // ✅ STATES
                Success = "#22C55E",
                Warning = "#FBBF24",
                Error = "#F87171",
                Info = "#60A5FA",

                SuccessContrastText = "#FFFFFF",
                WarningContrastText = "#000000",
                ErrorContrastText = "#FFFFFF",
                InfoContrastText = "#FFFFFF",

                // 📏 UI
                Divider = "#1F2937",

                // 🧱 LAYOUT
                AppbarBackground = "#111827",
                AppbarText = "#E2E8F0",

                DrawerBackground = "#111827",
                DrawerText = "#E2E8F0",

                // Overlay
                OverlayDark = "rgba(0,0,0,0.6)",
                OverlayLight = "rgba(17,24,39,0.5)"
            },

            // 🌫️ SHADOWS (viel moderner)

            LayoutProperties = new LayoutProperties
            {
                DefaultBorderRadius = "10px",   // moderner!
                AppbarHeight = "72px"
            },

            // ✍️ TYPOGRAPHY (wichtiger als man denkt!)
            Typography = new Typography
            {
                Default = new DefaultTypography
                {
                    FontFamily = new[]
                    {
                    "Inter",
                    "-apple-system",
                    "BlinkMacSystemFont",
                    "Segoe UI",
                    "Roboto",
                    "Arial",
                    "sans-serif"
                },
                    FontSize = "0.875rem",
                    LineHeight = "1.5"
                },

                H1 = new H1Typography
                {
                    FontSize = "2.5rem",
                    FontWeight = "700"
                },
                H2 = new H2Typography
                {
                    FontSize = "2rem",
                    FontWeight = "600"
                },
                H3 = new H3Typography
                {
                    FontSize = "1.75rem",
                    FontWeight = "600"
                },
                H4 = new H4Typography
                {
                    FontSize = "1.5rem",
                    FontWeight = "600"
                },
                H5 = new H5Typography
                {
                    FontSize = "1.25rem",
                    FontWeight = "500"
                },
                H6 = new H6Typography
                {
                    FontSize = "1rem",
                    FontWeight = "500"
                },

                Button = new ButtonTypography
                {
                    FontWeight = "600",
                    TextTransform = "none" // moderner als UPPERCASE
                }
            }
        };
    }
}
