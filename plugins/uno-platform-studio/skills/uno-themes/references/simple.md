# Uno Simple Theme

The Simple theme is a lightweight design system with minimal, essential styling, and the theme `dotnet new unoapp -preset recommended` selects. It shares the semantic surface (style keys, typography keys, color keys, brush keys) with Material via the **Semantic Design Language** — see `references/semantic-colors-brushes.md` for the shared layer. This reference covers the **Simple-only** style keys, brushes, and `SimpleTheme` configuration that are not portable to other themes.

## Workflow

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
- **Simple defaults to Inter** for all typography; `CharacterSpacing` is 0 where the M3 scale defines it (Display, Body, Label, Caption); the type scales use `Bold` / `SemiBold` / `Normal` (no `Medium`), but `SimpleButtonFontWeight` and `SimpleToggleButtonFontWeight` are `Medium`. `SimpleFontFamily` no longer exists; swap the font with `DefaultFontFamily` on the theme (see Customization).
- **Typography style keys are NEVER theme-prefixed.** Write `DisplayLarge`, `BodyMedium`, `LabelSmall` — NOT `SimpleDisplayLarge`. `SimpleBodyMedium` exists but does not resolve under Material; use the `Simple*` prefix only for the Simple-only control styles listed below.
- **Sizes come from design tokens, not size-named styles.** `DefaultDensity` (`Compact` / `Regular` / `Comfy`, spacing ×0.75 / ×1 / ×1.25) and `DefaultSpacing` scale the `Space*` tokens, `DefaultCornerRadius` scales the `Radius*` tokens (`RadiusFull` stays 9999), and `ControlHeight*`, `IconSize*`, and `TouchTargetMinSize` are fixed. There is no `DefaultSize` and no `SimpleSmall*` / `SimpleMedium*` style family in Uno.Themes 8.0; the one size variant is `SimpleTextBoxSmallStyle`.

## SimpleTheme Configuration

`SimpleTheme` (namespace `using:Uno.Simple`) accepts:

| Property | Type | Default | Purpose |
|---|---|---|---|
| `Colors` | `ThemeColors` (`using:Uno.Themes`) | — | Palette overrides: `OverrideSource` (URI of a XAML ResourceDictionary overriding `*Color` keys) or `OverrideDictionary` (inline). Replaces the obsolete `ColorOverrideSource` / `ColorOverrideDictionary` |
| `DefaultFontFamily` | `FontFamily` | — (Inter) | App-wide font swap: generates the `DefaultFontFamily` token and every type-scale `*FontFamily` key from one value. Use a variable font or one with a font manifest so the `*FontWeight` tokens render |
| `FontOverrideSource` | `string` (URI) | — | Path to a XAML ResourceDictionary overriding individual font resources |
| `FontOverrideDictionary` | `ResourceDictionary` | — | Inline ResourceDictionary overriding font resources |
| `DefaultDensity` | `Density` | `Regular` | `Compact` / `Regular` / `Comfy`: scales the `Space*` tokens ×0.75 / ×1 / ×1.25 |
| `DefaultCornerRadius` | `double` | `4` | Base value the `Radius*` tokens derive from |
| `DefaultSpacing` | `double` | `4` | Base unit the `Space*` tokens derive from |

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

With Uno Toolkit present, the element is `<utus:SimpleToolkitTheme xmlns:utus="using:Uno.Toolkit.UI.Simple" />` instead, which the recommended preset emits. It derives from `SimpleTheme`, so set the same properties on it, and never add a bare `SimpleTheme` next to it.

Enable the theme with `<UnoFeatures>SimpleTheme</UnoFeatures>` (`SimpleTheme;Toolkit` with the Toolkit; the recommended template already does).

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

These reference a Simple-specific **pink primitive scale**: `SimplePink200Color` through `SimplePink800Color`. The pink primitives hold the same values in Light and Dark; the utility brushes **swap which tone they use** (the measurement background is Pink 200 in Light and Pink 800 in Dark) to maintain contrast.

## Customization Methods

Simple supports five escalating overrides (ordered by scope):

1. **Seed or override the color palette (full cascade)** — `PrimarySeed` (and `SecondarySeed` / `TertiarySeed`), or `OverrideSource` / `OverrideDictionary`, on the `ThemeColors` in `SimpleTheme.Colors`. All brushes cascade.
2. **Density, spacing, corner radius** — `DefaultDensity="Compact|Regular|Comfy"`, `DefaultCornerRadius`, `DefaultSpacing` on `SimpleTheme`; every control follows through the `Space*` / `Radius*` tokens (`ControlHeight*` is fixed).
3. **Override specific brushes (targeted)** — `<SolidColorBrush x:Key="FilledButtonBackground" Color="..." />` in App.xaml.
4. **Override per-control instance (scoped)** — wrap the brush override inside the control's `Resources` block.
5. **Override font family** — set `DefaultFontFamily` on the theme (`<SimpleTheme DefaultFontFamily="ms-appx:///Fonts/MyFont.ttf#MyFont" />`, a variable font or one with a font manifest); use `FontOverrideSource` / `FontOverrideDictionary` only to override individual font keys. `SimpleFontFamily`, the per-weight `Simple*FontFamily` keys, and the 7.1.1 `TypefacePlain` / `TypefaceBrand` pair no longer exist; an override that still defines them is silently ignored (Uno.Themes 8.0 migration guide). The docs site still serves the 7.x migration page, which recommends `TypefacePlain`; ignore that on 8.0. Default is **Inter**.

Customization precedence (highest → lowest): per-instance `Control.Resources` → `Page.Resources` → app-level overrides → `SimpleTheme` defaults → `SharedColorPalette` / `SharedColors` / `SharedTypography` foundation.

## Related Skills

- `references/semantic-colors-brushes.md` — **Read this first** for the shared semantic surface (style keys, typography, palette, brushes) common to Simple AND Material. Most styling should use those portable keys; reach for Simple-prefixed styles only when targeting Simple-specific concepts (danger variants, Expander, PersonPicture, etc.).
- `references/material.md` — Material theme reference. Use when targeting Material instead of (or alongside) Simple.
