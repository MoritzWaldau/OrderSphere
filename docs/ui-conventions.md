# OrderSphere — UI & Styling Guide

Binding reference for all visual, theming, MudBlazor, and CSS work in `src/Frontend/OrderSphere.Web`.
The design direction is **"Flat & Focused"** (indigo). This document is the source of truth;
where it and older notes disagree, this document wins.

The canonical implementations are:

- Theme (palette, typography, layout): `src/Frontend/OrderSphere.Web/Services/ThemeState.cs`
- Design tokens and component classes: `src/Frontend/OrderSphere.Web/wwwroot/app.css`

Change those two files first; this guide documents what they contain.

---

## 1. Design principles

- **Flat surfaces, no chrome.** `Elevation="0"` everywhere; separation comes from 1px dividers
  and the `--os-shadow-*` token shadows, not Material elevation.
- **One gradient.** The indigo gradient appears on the hero and CTA sections only
  (`--os-gradient-primary` / `--os-gradient-primary-reverse`). Everything else is a flat surface.
- **Two type roles.** Space Grotesk for display/headings, JetBrains Mono for numeric and
  metadata (prices, quantities, category/eyebrow labels, legal links).
- **Light-only.** The application ships without a theme switch. A `PaletteDark` and
  `[data-mud-theme="dark"]` token overrides exist in code but are not surfaced in the UI;
  do not add a dark-mode toggle.
- **Tokens over hardcoded values.** Use `var(--mud-palette-*)` for theme colors and
  `var(--os-*)` for radii, shadows, gradients, and spacing. Avoid literal hex in components.

---

## 2. Palette (`ThemeState.cs`, `PaletteLight`)

| Token | Value | Use |
|---|---|---|
| Primary | `#3A4DD1` | Buttons, links, accents, focus |
| PrimaryDarken | `#2B3BB8` | Gradient end, hover |
| PrimaryLighten | `#6A7AE8` | Subtle accents, hover borders |
| Secondary | `#0D0D18` | Off-black; `section-dark`, strong text |
| Background | `#FFFFFF` | Page background |
| BackgroundGray | `#F1F2F8` | Alternating sections, inert fills |
| Surface | `#FFFFFF` | Cards, paper |
| TextPrimary | `#0D0D18` | Headings, primary text |
| TextSecondary | `#5A5A72` | Muted body / captions |
| Success | `#2EA04B` | Stock OK, free shipping |
| Warning | `#FF9F0A` | Low stock |
| Error | `#FF3B30` | Errors, destructive actions |
| Info | `#3A4DD1` | Informational (same as Primary) |
| Divider | `#DCDDE6` | Borders, separators |
| DividerLight | `#E8E9F0` | Hairline rows |

---

## 3. Typography

Font stacks are defined in `ThemeState.cs`:

- **Display / headings:** `Space Grotesk → Inter → system sans`. Helper class: `.os-display`.
- **Monospace / numeric:** `JetBrains Mono → ui-monospace → monospace`. Helper class: `.os-mono`.

Headings (H1–H6) use Space Grotesk at weight 600 with negative letter-spacing (tightest on
H1/H2). Buttons use `TextTransform = "none"`, weight 600 — never uppercase button labels.

Monospace is reserved for:

- Prices — `.os-price` (tabular numerals, weight 700) or `.os-mono`.
- Eyebrows — `.os-eyebrow` (mono, 11.5px, uppercase, leading rule, accent color).
- Category labels and the footer legal links.

---

## 4. Layout & radii

`LayoutProperties`: `DefaultBorderRadius = 12px`, `AppbarHeight = 72px`.

Radius tokens (`app.css`):

| Token | Value | Use |
|---|---|---|
| `--os-radius-sm` | 8px | Small chips, thumbnails |
| `--os-radius-md` | 14px | Inputs, inner cards |
| `--os-radius-lg` | 20px | Page cards, panels |
| `--os-radius-pill` | 100px | Buttons, quantity controls |

Section vertical rhythm uses `--os-section-py-sm|md|lg` (60/80/100px).

---

## 5. Section variants (`app.css`)

| Class | Background |
|---|---|
| `section-hero` / `section-hero-sm` | Indigo gradient (`--os-gradient-primary`) |
| `section-cta` | Reversed indigo gradient |
| `section-gray` | `BackgroundGray` |
| `section-white` | `Surface` |
| `section-dark` | `Secondary` off-black |

On gradient/dark backgrounds use the hero text helpers: `.hero-title`, `.hero-title-lg`,
`.hero-subtitle`, `.hero-subtitle-muted`, `.text-on-dark`, `.text-on-dark-muted`, and the
`.hero-pill-soft` chip. The `PageHero` component wraps the common hero layout.

---

## 6. Components

### Buttons
| Context | Class |
|---|---|
| Standard CTA | `btn-pill` |
| On gradient/dark background | `btn-pill-white` / `btn-pill-outline-white` |
| Neutral outline on light surface | `btn-pill-outline` |
| Form submit | `btn-pill` + `FullWidth="true"` |

### Surfaces
- Use `surface-card`, `surface-card-sm`, `surface-card-lg` (border + radius + surface) instead
  of `MudPaper` elevation. Add `surface-card-row-hover` for clickable rows.
- Product cards: `product-card` with `product-card-image` / `product-card-body`; the round add
  button is `product-add-btn`.
- Category grid: `category-card`; flat category strip: `category-strip-*`.

### Header & footer
- Header: `MudAppBar Elevation="0" Class="header-appbar"` (frosted glass). Outlined icons only.
  Logo mark: `.os-logo-mark`. Active nav underline: `.nav-link-active`.
- Footer: `.footer-main`, `.footer-link`, `.footer-bottom`, `.footer-legal-link` (mono).

### Checkout & cart
- Reusable summary: the `OrderSummary` component (cart and checkout share it). Set
  `ShowLineItems` to render the itemised list; pass page-specific buttons via the `Actions`
  fragment.
- Checkout sub-forms: `CheckoutAddressForm` and `CheckoutPaymentForm`, both bound to the shared
  `CheckoutFormModel`.
- Checkout CSS: `checkout-section`, `checkout-step-num`, `shipping-option`, `radio-dot`.

### Status, misc
- Stock: `stock-ok`, `stock-low`, `stock-out`; free shipping: `shipping-free`.
- Icon bubbles: `icon-bubble-primary|success|success-soft|on-hero`; initials: `avatar-initials`.
- Destructive zones: `danger-zone` / `danger-zone-title`.

---

## 7. Text emphasis

Use `Class="text-muted"` (maps to `TextSecondary`) for muted body and caption text rather than
`Color="Color.Secondary"`. In this palette `Secondary` is near-`TextPrimary` off-black and
produces no hierarchy. Use `text-strong` for emphasis back to primary text.

---

## 8. Fonts (preconnect in `index.html`)

```html
<link rel="preconnect" href="https://fonts.googleapis.com">
<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
<link href="https://fonts.googleapis.com/css2?family=Space+Grotesk:wght@400;500;600;700&family=JetBrains+Mono:wght@400;500;600;700&display=swap" rel="stylesheet">
```
