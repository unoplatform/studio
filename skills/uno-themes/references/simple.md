# Uno Simple Theme

The Simple theme is a lightweight design system with minimal, essential styling, and the theme `dotnet new unoapp -preset recommended` selects. It shares the semantic surface (style keys, typography keys, color keys, brush keys) with Material via the **Semantic Design Language** — see `references/semantic-colors-brushes.md` for the shared layer. This reference covers the **Simple-only** style keys, brushes, and `SimpleTheme` configuration that are not portable to other themes.

## Workflow

> **Docs lookup:** call `uno_platform_docs_search(...)` first, then `uno_platform_docs_fetch(sourcePath="…")` using the `sourcePath` field from a result (a relative `.md` path; add the result's `anchor` for a section). Never pass a URL, a `.html` link, or a hand-built path.

### Step 1: Search the docs for the topic

| Topic | Search query |
|---|---|
| Installation / `SimpleTheme` setup | `uno_platform_docs_search("Uno Simple theme installation SimpleTheme UnoFeatures")` |
| Simple style keys (buttons, inputs, etc.) | `uno_platform_docs_search("Uno Simple controls styles danger")` |
| Density, spacing, corner radius, design tokens | `uno_platform_docs_search("Uno Themes DefaultDensity design tokens Space Radius ControlHeight")` |
| Seed colors | `uno_platform_docs_search("Uno Themes seed colors PrimarySeed ThemeColors")` |
| Color override / palette | `uno_platform_docs_search("Uno Themes ThemeColors OverrideSource palette")` |
| Font override | `uno_platform_docs_search("Uno Simple FontOverrideSource Inter font family")` |
| Utility brushes (overlays, scrims) | `uno_platform_docs_search("Uno Simple utility brushes overlay scrim measurement")` |

### Step 2: Fetch a page

- **Simple Getting Started**: `external/uno.themes/doc/simple-getting-started.md`
- **Simple Controls Styles** (authoritative key list): `external/uno.themes/doc/simple-controls-styles.md`
- **Semantic styles**: `external/uno.themes/doc/semantic-styles.md`
- **Design tokens** (`Space*`, `Radius*`, `ControlHeight*`, density): `external/uno.themes/doc/design-tokens.md`
- **Seed colors**: `external/uno.themes/doc/seed-colors.md`
- **Themes overview**: `external/uno.themes/doc/themes-overview.md`
- **Lightweight Styling** (cross-theme): `external/uno.themes/doc/lightweight-styling.md`

```
uno_platform_docs_fetch(sourcePath="external/uno.themes/doc/simple-controls-styles.md")
```

The inline tables below are a fast lookup; `simple-controls-styles.md` wins when they disagree.

## Critical Rules

- **Simple uses a flat grayscale palette** for all roles except Error (which uses red). Primary, Secondary, Tertiary, Surface, and Outline are achromatic grays. To introduce brand colors, set seeds or override the palette through `SimpleTheme.Colors` (`<ut:ThemeColors PrimarySeed="..." />` or `OverrideSource`); `ColorOverrideSource` / `ColorOverrideDictionary` are obsolete.
- **Simple defaults to Inter** for all typography; `CharacterSpacing` is 0 where the M3 scale defines it (Display, Body, Label, Caption); font weights are `Bold` / `SemiBold` / `Normal` (no `Medium`). `SimpleFontFamily` is a legacy key no type-scale slot reads; override fonts through `FontOverrideSource` (see Customization).
- **Typography style keys are NEVER theme-prefixed.** Write `DisplayLarge`, `BodyMedium`, `LabelSmall` — NOT `SimpleDisplayLarge`. The theme-prefix pattern only applies to the Simple-only control styles listed below.
- **Sizes come from design tokens, not size-named styles.** `DefaultDensity` (`Compact` / `Regular` / `Comfy`, spacing ×0.75 / ×1 / ×1.25) and `DefaultCornerRadius` on the theme scale the `Space*`, `Radius*`, and `ControlHeight*` tokens every control reads. There is no `DefaultSize` and no `SimpleSmall*` / `SimpleMedium*` style family (they were removed in Uno.Themes 7.0.3).

## SimpleTheme Configuration

`SimpleTheme` (namespace `using:Uno.Simple`) accepts:

| Property | Type | Default | Purpose |
|---|---|---|---|
| `Colors` | `ThemeColors` (`using:Uno.Themes`) | — | Palette overrides: `OverrideSource` (URI of a XAML ResourceDictionary overriding `*Color` keys) or `OverrideDictionary` (inline). Replaces the obsolete `ColorOverrideSource` / `ColorOverrideDictionary` |
| `FontOverrideSource` | `string` (URI) | — | Path to a XAML ResourceDictionary overriding font resources |
| `FontOverrideDictionary` | `ResourceDictionary` | — | Inline ResourceDictionary overriding font resources |
| `DefaultDensity` | `Density` | `Regular` | `Compact` / `Regular` / `Comfy`: scales the spacing tokens ×0.75 / ×1 / ×1.25 |
| `DefaultCornerRadius` | `CornerRadius` | theme default | Base corner radius the `Radius*` tokens derive from |
| `DefaultSpacing` | `double` | theme default | Base spacing unit (Uno.Themes 8.0 / main) |

```xml
<SimpleTheme xmlns="using:Uno.Simple"
             FontOverrideSource="ms-appx:///Styles/Application/FontOverride.xaml"
             DefaultDensity="Compact">
    <SimpleTheme.Colors>
        <ut:ThemeColors xmlns:ut="using:Uno.Themes"
                        OverrideSource="ms-appx:///Styles/Application/ColorOverride.xaml" />
    </SimpleTheme.Colors>
</SimpleTheme>
```

When overriding the palette, **always provide both `Light` and `Default` (Dark) theme values**. For a brand color without a palette file, set seeds instead: `<ut:ThemeColors PrimarySeed="#0F62FE" />` (`SecondarySeed`, `TertiarySeed` optional; `SeedColorMode` on 8.0).

Enable the theme with `<UnoFeatures>SimpleTheme</UnoFeatures>` (the recommended template already does).

## Simple-Only Style Keys

### Button Variants (no Material equivalent)

| Style Key | Control | Use For |
|---|---|---|
| `SimpleDangerPrimaryButtonStyle` | `Button` | Destructive filled action ("Delete account") |
| `SimpleDangerSubtleButtonStyle` | `Button` | Destructive text action |
| `SimpleIconButtonNeutralStyle` | `Button` | Neutral (secondary) icon button |
| `SimpleIconButtonSubtleStyle` | `Button` | Subtle (tertiary) icon button |
| `SimpleIconButtonDangerPrimaryStyle` | `Button` | Destructive filled icon button |
| `SimpleIconButtonDangerSubtleStyle` | `Button` | Destructive subtle icon button |

### Input Variants

| Style Key | Control | Use For |
|---|---|---|
| `SimpleTextBoxErrorStyle` | `TextBox` | Error / validation state |
| `SimpleTextBoxSmallStyle` | `TextBox` | Small size variant |
| `SimpleComboBoxErrorStyle` | `ComboBox` | Error / validation state |

### Controls Without Material Equivalent

| Style Key | Control | Notes |
|---|---|---|
| `SimpleExpanderStyle` | `Expander` | No Material counterpart |
| `SimpleAutoSuggestBoxStyle` | `AutoSuggestBox` | No Material counterpart |
| `SimpleToolTipStyle` | `ToolTip` | No Material counterpart |
| `SimplePersonPictureStyle` | `PersonPicture` | Circular, medium (default) |
| `SimplePersonPictureSmallStyle` | `PersonPicture` | Circular, small |
| `SimplePersonPictureLargeStyle` | `PersonPicture` | Circular, large |
| `SimplePersonPictureSquareStyle` | `PersonPicture` | Square (rounded), medium |
| `SimplePersonPictureSquareSmallStyle` | `PersonPicture` | Square (rounded), small |
| `SimplePersonPictureSquareLargeStyle` | `PersonPicture` | Square (rounded), large |

## Simple-Specific Utility Brushes

Beyond the shared semantic brushes, Simple defines utility brushes for overlays, scrims, and measurement/debug overlays. Defined in Simple's `ColorPalette.xaml`; no shared equivalent.

| Brush Key | Light | Dark | Purpose |
|---|---|---|---|
| `SimpleBackgroundUtilitiesBlanketBrush` | `#B2000000` | `#B2000000` | Heavy overlay / blanket |
| `SimpleBackgroundUtilitiesMeasurementBrush` | Pink 200 | Pink 800 | Measurement overlay background |
| `SimpleBackgroundUtilitiesOverlayBrush` | `#80000000` | `#80000000` | Semi-transparent overlay |
| `SimpleBackgroundUtilitiesScrimBrush` | `#CCFFFFFF` | `#CC000000` | Scrim behind dialogs / sheets |
| `SimpleTextUtilitiesOnMeasurementBrush` | Pink 800 | Pink 200 | Text on measurement surfaces |
| `SimpleTextUtilitiesOnOverlayBrush` | `OnSurfaceInverseBrush` | `OnSurfaceBrush` | Text on overlay surfaces |
| `SimpleIconUtilitiesBrush` | Pink 600 | Pink 400 | Measurement / debug icons |
| `SimpleIconUtilitiesOnMeasurementBrush` | Pink 800 | Pink 200 | Icons on measurement surfaces |
| `SimpleBorderUtilitiesMeasurementBrush` | Pink 400 | Pink 600 | Measurement / debug borders |
| `SimpleBorderUtilitiesSwatchBrush` | `#3D000000` | `#3DFFFFFF` | Color swatch borders |

These reference a Simple-specific **pink primitive scale**: `SimplePink200Color` through `SimplePink800Color`. Pink values **intentionally swap** between light and dark themes to maintain contrast.

## Customization Methods

Simple supports five escalating overrides (ordered by scope):

1. **Seed or override the color palette (full cascade)** — `PrimarySeed` (and `SecondarySeed` / `TertiarySeed`), or `OverrideSource` / `OverrideDictionary`, on the `ThemeColors` in `SimpleTheme.Colors`. All brushes cascade.
2. **Density, spacing, corner radius** — `DefaultDensity="Compact|Regular|Comfy"`, `DefaultCornerRadius`, `DefaultSpacing` on `SimpleTheme`; every control follows through the `Space*` / `Radius*` / `ControlHeight*` tokens.
3. **Override specific brushes (targeted)** — `<SolidColorBrush x:Key="FilledButtonBackground" Color="..." />` in App.xaml.
4. **Override per-control instance (scoped)** — wrap the brush override inside the control's `Resources` block.
5. **Override font family** — `FontOverrideSource` / `FontOverrideDictionary` on `SimpleTheme` with a dictionary that redefines `DefaultFontFamily` (works on 7.x and 8.0). On 7.x the per-weight keys `SimpleRegularFontFamily` / `SimpleSemiBoldFontFamily` / `SimpleBoldFontFamily` also work; `SimpleFontFamily` is legacy and changes nothing. Default is **Inter**.

Customization precedence (highest → lowest): per-instance `Control.Resources` → `Page.Resources` → app-level overrides → `SimpleTheme` defaults → `SharedColorPalette` / `SharedColors` / `SharedTypography` foundation.

## Related Skills

- `references/semantic-colors-brushes.md` — **Read this first** for the shared semantic surface (style keys, typography, palette, brushes) common to Simple AND Material. Most styling should use those portable keys; reach for Simple-prefixed styles only when targeting Simple-specific concepts (danger variants, Expander, PersonPicture, etc.).
- `references/material.md` — Material theme reference. Use when targeting Material instead of (or alongside) Simple.
