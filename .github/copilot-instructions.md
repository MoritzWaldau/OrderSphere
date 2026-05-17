# OrderSphere – Styling Guide

## 1. Design Philosophie

Orientiert an **Apple.com** und **Amazon** – klare Kontraste, großzügige Weißräume, subtile Schatten und runde Formen. Kein Uppercase, keine harten Kanten.

---

## 2. MudBlazor Theme (`ThemeConfiguration.cs`)

```csharp
public static MudTheme ApplicationTheme()
{
    return new MudTheme()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#0071E3",                    // Apple Blau
            PrimaryContrastText = "#FFFFFF",
            PrimaryDarken = "#0051A8",
            PrimaryLighten = "#3395FF",

            Secondary = "#1D1D1F",
            SecondaryContrastText = "#FFFFFF",
            SecondaryDarken = "#000000",
            SecondaryLighten = "#3D3D3F",

            Tertiary = "#06C167",
            TertiaryContrastText = "#FFFFFF",

            Background = "#FFFFFF",
            BackgroundGray = "#F5F5F7",
            Surface = "#FFFFFF",

            TextPrimary = "#1D1D1F",
            TextSecondary = "#6E6E73",
            TextDisabled = "rgba(29,29,31,0.30)",

            Success = "#06C167",
            Warning = "#FF9F0A",
            Error = "#FF3B30",
            Info = "#0071E3",

            SuccessContrastText = "#FFFFFF",
            WarningContrastText = "#FFFFFF",
            ErrorContrastText = "#FFFFFF",
            InfoContrastText = "#FFFFFF",

            Divider = "#D2D2D7",
            DividerLight = "#E8E8ED",

            HoverOpacity = 0.06,
            RippleOpacity = 0.08,

            AppbarBackground = "rgba(255,255,255,0.85)",
            AppbarText = "#1D1D1F",

            DrawerBackground = "#FFFFFF",
            DrawerText = "#1D1D1F",
            DrawerIcon = "#1D1D1F",

            LinesDefault = "#D2D2D7",
            TableLines = "#D2D2D7",
            TableStriped = "rgba(0,0,0,0.02)",
            TableHover = "rgba(0,0,0,0.04)",

            ActionDefault = "#1D1D1F",
            ActionDisabled = "rgba(29,29,31,0.30)",
            ActionDisabledBackground = "rgba(29,29,31,0.08)",

            OverlayDark = "rgba(0,0,0,0.5)",
            OverlayLight = "rgba(255,255,255,0.7)",
        },

        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "12px",
            AppbarHeight = "72px",
        },

        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = new[] { "Inter", "-apple-system", "BlinkMacSystemFont", "SF Pro Display", "Segoe UI", "Roboto", "Arial", "sans-serif" },
                FontSize = "0.9375rem",
                FontWeight = "400",
                LineHeight = "1.6"
            },
            H1 = new H1Typography { FontSize = "3rem", FontWeight = "700", LineHeight = "1.1", LetterSpacing = "-0.02em" },
            H2 = new H2Typography { FontSize = "2.25rem", FontWeight = "700", LineHeight = "1.15", LetterSpacing = "-0.015em" },
            H3 = new H3Typography { FontSize = "1.875rem", FontWeight = "600", LineHeight = "1.2", LetterSpacing = "-0.01em" },
            H4 = new H4Typography { FontSize = "1.5rem", FontWeight = "600", LineHeight = "1.25", LetterSpacing = "-0.01em" },
            H5 = new H5Typography { FontSize = "1.25rem", FontWeight = "600", LineHeight = "1.3" },
            H6 = new H6Typography { FontSize = "1.0625rem", FontWeight = "600", LineHeight = "1.4" },
            Body1 = new Body1Typography { FontSize = "0.9375rem", FontWeight = "400", LineHeight = "1.6" },
            Body2 = new Body2Typography { FontSize = "0.8125rem", FontWeight = "400", LineHeight = "1.5" },
            Subtitle1 = new Subtitle1Typography { FontSize = "1rem", FontWeight = "500", LineHeight = "1.5" },
            Subtitle2 = new Subtitle2Typography { FontSize = "0.875rem", FontWeight = "500", LineHeight = "1.5" },
            Caption = new CaptionTypography { FontSize = "0.75rem", FontWeight = "400", LineHeight = "1.4", LetterSpacing = "0.01em" },
            Button = new ButtonTypography { FontSize = "0.9375rem", FontWeight = "500", TextTransform = "none", LetterSpacing = "0" },
            Overline = new OverlineTypography { FontSize = "0.6875rem", FontWeight = "500", LetterSpacing = "0.08em", LineHeight = "1.4" }
        }
    };
}
```

---

## 3. Custom CSS (`app.css`)

```css
/* ============================================
   SCROLL LOCK
   ============================================ */
body.scroll-locked {
    overflow: hidden;
    padding-right: var(--scrollbar-width, 0px);
}

/* ============================================
   HEADER – Frosted Glass
   ============================================ */
.header-appbar {
    background: rgba(255, 255, 255, 0.85) !important;
    backdrop-filter: saturate(180%) blur(20px);
    -webkit-backdrop-filter: saturate(180%) blur(20px);
    border-bottom: 1px solid var(--mud-palette-divider);
}

/* ============================================
   PRODUCT CARD
   ============================================ */
.product-card {
    transition: box-shadow 0.25s ease, transform 0.25s ease;
}

.product-card:hover {
    box-shadow: 0 12px 32px rgba(0, 0, 0, 0.12) !important;
    transform: translateY(-4px);
}

.product-image-wrapper {
    position: relative;
    overflow: hidden;
}

.product-overlay {
    position: absolute;
    inset: 0;
    background: rgba(0, 0, 0, 0.45);
    display: flex;
    align-items: center;
    justify-content: center;
    opacity: 0;
    transition: opacity 0.25s ease;
}

.product-image-wrapper:hover .product-overlay {
    opacity: 1;
}

/* ============================================
   SECTIONS
   ============================================ */
.section-hero {
    background: linear-gradient(135deg, #0071E3 0%, #0051A8 100%);
    padding: 100px 0;
}

.section-hero-sm {
    background: linear-gradient(135deg, #0071E3 0%, #0051A8 100%);
    padding: 60px 0;
}

.section-gray {
    background: var(--mud-palette-background-grey);
    padding: 80px 0;
}

.section-white {
    background: var(--mud-palette-surface);
    padding: 80px 0;
}

.section-dark {
    background: #1D1D1F;
    padding: 80px 0;
}

.section-cta {
    background: linear-gradient(135deg, #0051A8 0%, #0071E3 100%);
    padding: 100px 0;
}

/* ============================================
   BUTTONS – Pill Style
   ============================================ */
.btn-pill {
    border-radius: 100px !important;
    padding: 12px 32px !important;
    font-weight: 600 !important;
}

.btn-pill-white {
    background: #FFFFFF !important;
    color: #0071E3 !important;
    border-radius: 100px !important;
    padding: 12px 32px !important;
    font-weight: 600 !important;
}

.btn-pill-outline-white {
    border-color: rgba(255, 255, 255, 0.6) !important;
    color: #FFFFFF !important;
    border-radius: 100px !important;
    padding: 12px 32px !important;
}

/* ============================================
   CART DRAWER
   ============================================ */
.mud-drawer-content {
    display: flex;
    flex-direction: column;
    height: 100%;
    overflow: hidden;
}

.cart-quantity-control {
    display: inline-flex;
    align-items: center;
    background: var(--mud-palette-background-grey);
    border-radius: 100px;
    padding: 2px;
}

/* ============================================
   FOOTER
   ============================================ */
.footer-main {
    background: var(--mud-palette-background-grey);
    padding: 60px 0 40px 0;
    border-top: 1px solid var(--mud-palette-divider);
}

.footer-bottom {
    background: #1D1D1F;
    padding: 20px 0;
}

.footer-link {
    color: var(--mud-palette-text-secondary);
    font-size: 0.875rem;
    text-decoration: none;
    transition: color 0.15s ease;
}

.footer-link:hover {
    color: var(--mud-palette-primary);
}

.footer-bottom-link {
    color: #98989D;
    font-size: 0.75rem;
    text-decoration: none;
    transition: color 0.15s ease;
}

.footer-bottom-link:hover {
    color: #FFFFFF;
}

/* ============================================
   MOBILE DRAWER NAV
   ============================================ */
.mobile-nav-item {
    display: flex;
    align-items: center;
    padding: 12px;
    border-radius: 12px;
    margin-bottom: 4px;
    cursor: pointer;
    transition: background 0.15s ease;
    text-decoration: none;
}

.mobile-nav-item:hover {
    background: var(--mud-palette-background-grey);
}

/* ============================================
   CATEGORY CARD
   ============================================ */
.category-card {
    border-radius: 16px;
    border: 1px solid var(--mud-palette-divider);
    background: var(--mud-palette-surface);
    cursor: pointer;
    transition: box-shadow 0.2s ease;
}

.category-card:hover {
    box-shadow: 0 8px 24px rgba(0, 0, 0, 0.12);
}
```

---

## 4. JavaScript (`app.js`)

```javascript
// Scroll Lock – genutzt wenn CartDrawer offen ist
window.lock = () => {
    document.body.style.overflow = 'hidden';
    document.body.style.paddingRight =
        window.innerWidth - document.documentElement.clientWidth + 'px';
};

window.unlock = () => {
    document.body.style.overflow = '';
    document.body.style.paddingRight = '';
};
```

Einbindung in `App.razor` – **nach** `blazor.web.js`:
```html
<script src="~/app.js"></script>
```

---

## 5. Layout Patterns

### Seiten-Sektionen
Seiten sind in `<section>` Tags mit CSS-Klassen aufgeteilt. Abwechselnde Hintergründe für visuellen Rhythmus (Apple-Stil):

```
Hero (Blauer Gradient)
  ↓
Kategorien (Hellgrau)
  ↓
Benefits (Weiß)
  ↓
AI Section (Dunkel #1D1D1F)
  ↓
CTA (Umgekehrter Gradient)
```

### Auth-Seiten Layout
Zentrierte Card auf grauem Hintergrund, `min-height: 100vh`:

```razor
<section class="section-gray" style="min-height: 100vh; display: flex; align-items: center;">
    <MudContainer MaxWidth="MaxWidth.Small">
        <MudPaper Elevation="0" Class="pa-8"
                  Style="border-radius: 20px; border: 1px solid var(--mud-palette-divider);">
            <!-- Inhalt -->
        </MudPaper>
    </MudContainer>
</section>
```

### Gefahrenzone (Account löschen)
Roter Border statt normalem Divider:

```razor
<MudPaper Elevation="0" Class="pa-6"
          Style="border-radius: 20px; border: 1px solid #FECACA;">
```

---

## 6. Komponentenstile

### Header (Frosted Glass)
```razor
<MudAppBar Elevation="0" Fixed="true" Class="header-appbar">
```
- `Elevation="0"` – kein Shadow (Border übernimmt die Trennung)
- Icons: `Icons.Material.Outlined.*` (nicht Filled)
- Nav-Buttons: `Color="Color.Dark"` für Light Mode

### Buttons
| Kontext | Klasse | Beispiel |
|---|---|---|
| Standard CTA | `btn-pill` | Anmelden, Speichern |
| Hero auf farbigem BG | `btn-pill-white` | "Jetzt entdecken" |
| Sekundär auf farbigem BG | `btn-pill-outline-white` | "Mehr erfahren" |
| Formular Submit | `btn-pill` + `FullWidth="true"` | Login, Register |

### MudCard / MudPaper
- `Elevation="0"` bevorzugen, Border per `Style` oder CSS
- `border-radius: 16px` für Karten (leicht mehr als globale 12px)
- `border-radius: 20px` für Auth-Cards (noch weicher)

### Produkt-Karten
```razor
<MudPaper Elevation="0" Class="product-card"
          Style="border-radius: 16px; border: 1px solid var(--mud-palette-divider);">
    <div class="product-image-wrapper">
        <!-- Bild -->
        <div class="product-overlay">
            <!-- Hover Button -->
        </div>
    </div>
</MudPaper>
```

### CartDrawer
- `Width="420px"`, `Anchor="Anchor.Right"`
- Flexbox über `.mud-drawer-content` CSS
- Scrollbarer Bereich: `style="flex: 1; overflow-y: auto;"`
- Footer immer unten: `style="flex-shrink: 0;"`
- Quantity Controls: `.cart-quantity-control` Pill

### MobileDrawer
- Sektions-Label mit `Typo.Overline`
- Nav-Links mit `.mobile-nav-item` CSS-Klasse
- Kein `MudNavMenu` – stattdessen eigene `<div>` Links für mehr Kontrolle

---

## 7. Farb-Referenz (Schnellübersicht)

OrderSphere ist ausschließlich Light-Mode. Es gibt keinen Theme-Switcher und keine `PaletteDark`.

| Token | Wert | Verwendung |
|---|---|---|
| Primary | `#0071E3` | Buttons, Links, Akzente |
| Background | `#FFFFFF` | Seitenhintergrund |
| BackgroundGray | `#F5F5F7` | Sektionen, Cards |
| Surface | `#FFFFFF` | Cards, Paper |
| TextPrimary | `#1D1D1F` | Überschriften |
| TextSecondary | `#6E6E73` | Beschreibungen |
| Divider | `#D2D2D7` | Trennlinien |
| Success | `#06C167` | Verfügbarkeit |
| Error | `#FF3B30` | Fehler, Löschen |
| Dark Section | `#1D1D1F` | AI Section, Footer Bottom (off-black Akzent, keine Theme-Dunkelheit) |

---

## 8. Design-Entscheidungen

| Entscheidung | Begründung |
|---|---|
| `TextTransform = "none"` auf Buttons | Kein Uppercase – moderner, Apple-Stil |
| `Elevation="0"` + Border statt Shadow | Flacheres, moderneres Design |
| `border-radius: 100px` auf Buttons | Pill-Stil wie Apple.com |
| Outlined Icons im Header | Wirkt leichter als Filled |
| `backdrop-filter: blur` im Header | Frosted Glass wie Apple.com |
| Abwechselnde Sektionen hell/dunkel | Visuelle Trennung ohne harte Grenzen |
| `var(--mud-palette-*)` statt Hardcoded | Zentrale Palette als Source of Truth |
| Direktes CSS nur wenn nötig | MudBlazor Klassen bevorzugt |
| `LetterSpacing: -0.02em` bei H1/H2 | Engeres Spacing bei Headlines (Apple-Stil) |
| `#1D1D1F` für Dark Section / Footer | Apple "off-black" – weicher als reines Schwarz |

---

## 9. Fonts

```html
<!-- In App.razor <head> -->
<link href="https://fonts.googleapis.com/css2?family=Inter:wght@300;400;500;600;700&display=swap" rel="stylesheet">
```

Font-Stack: `Inter → -apple-system → BlinkMacSystemFont → SF Pro Display → Segoe UI → Roboto → Arial → sans-serif`