---
name: uno-themes-simple
description: "Comprehensive reference for the Uno Simple theme — designer/wireframe-style theming for Uno Platform apps. Covers SimpleTheme configuration (color/font overrides, default size), Simple-specific style keys (danger buttons, size variants, error inputs, PersonPicture variants), controls without Material equivalent (Expander, AutoSuggestBox, ToolTip, PersonPicture), Simple utility brushes (overlays, scrims, measurement/debug), and the Simple-specific pink primitive scale."
when_to_use: "Use when targeting the Uno Simple theme — setting up `SimpleTheme`, choosing Simple-only style keys (danger buttons, Small/Medium size variants, error inputs, PersonPicture variants), styling controls that exist only in Simple (Expander, AutoSuggestBox, ToolTip), overriding the Inter font or the grayscale palette, or working with Simple's utility brush family (overlays, scrims, measurement/debug)."
metadata:
  author: uno-platform
  version: "1.0"
  framework: uno-platform
  category: themes
---

# Uno Simple Theme — Agent Skill

The Simple theme is Uno's designer/wireframe-style theme. It shares the semantic surface (style keys, typography keys, color keys, brush keys) with Material via the **Semantic Design Language** — see `uno-themes-semantic-colors-brushes` for the shared layer. This skill covers the **Simple-only** style keys, brushes, and `SimpleTheme` configuration that are not portable to other themes.

## Workflow

> **Docs lookup:** call `uno_platform_docs_search(...)` first, then `uno_platform_docs_fetch(sourcePath="…")` using the `sourcePath` field from a result (a relative `.md` path; add the result's `anchor` for a section). Never pass a URL, a `.html` link, or a hand-built path.

### Step 1: Search the docs for the topic

| Topic | Search query |
|---|---|
| Installation / `SimpleTheme` setup | `uno_platform_docs_search("Uno Simple theme installation SimpleTheme UnoFeatures")` |
| Simple style keys (buttons, inputs, etc.) | `uno_platform_docs_search("Uno Simple controls styles danger size variants")` |
| `DefaultSize` (Small / Medium) | `uno_platform_docs_search("Uno Simple DefaultSize SimpleControlSize button height")` |
| Color override / palette | `uno_platform_docs_search("Uno Simple ColorOverrideSource palette grayscale")` |
| Font override | `uno_platform_docs_search("Uno Simple FontOverrideSource Inter font family")` |
| Utility brushes (overlays, scrims) | `uno_platform_docs_search("Uno Simple utility brushes overlay scrim measurement")` |

### Step 2: Fetch a page

Uno's published docs have **no Simple-theme-specific pages** — the inline tables in this skill are the authoritative reference for Simple. For cross-theme topics, search and fetch the `sourcePath` from a result, e.g. lightweight styling:

- **Lightweight Styling** (cross-theme): `external/uno.themes/doc/lightweight-styling.md`

```
uno_platform_docs_fetch(sourcePath="external/uno.themes/doc/lightweight-styling.md")
```

## Critical Rules

- **Simple uses a flat grayscale palette** for all roles except Error (which uses red). Primary, Secondary, Tertiary, Surface, and Outline are achromatic grays. To introduce brand colors, override the palette via `ColorOverrideSource` / `ColorOverrideDictionary`.
- **Simple defaults to Inter** for all typography (`SimpleFontFamily`); `CharacterSpacing=0` on all styles; font weights are `Bold` / `SemiBold` / `Normal` (no `Medium`).
- **Typography style keys are NEVER theme-prefixed.** Write `DisplayLarge`, `BodyMedium`, `LabelSmall` — NOT `SimpleDisplayLarge`. The theme-prefix pattern only applies to the Simple-only control styles listed below.
- **`DefaultSize` affects implicit styles only.** Setting `<SimpleTheme DefaultSize="Medium" />` switches the unsized button / icon-button / toggle-button defaults from 32px (Small) to 40px (Medium). Size-specific styles (`SimpleSmall*`, `SimpleMedium*`) always use their declared size regardless of `DefaultSize`.

## SimpleTheme Configuration

`SimpleTheme` (namespace `using:Uno.Simple`) accepts:

| Property | Type | Default | Purpose |
|---|---|---|---|
| `ColorOverrideSource` | `string` (URI) | — | Path to a XAML ResourceDictionary overriding `*Color` keys |
| `ColorOverrideDictionary` | `ResourceDictionary` | — | Inline ResourceDictionary overriding `*Color` keys |
| `FontOverrideSource` | `string` (URI) | — | Path to a XAML ResourceDictionary overriding font resources |
| `FontOverrideDictionary` | `ResourceDictionary` | — | Inline ResourceDictionary overriding font resources |
| `DefaultSize` | `SimpleControlSize` | `Small` | Default size variant for buttons / icon buttons / toggle buttons |

```xml
<SimpleTheme xmlns="using:Uno.Simple"
             ColorOverrideSource="ms-appx:///Styles/Application/ColorOverride.xaml"
             FontOverrideSource="ms-appx:///Styles/Application/FontOverride.xaml"
             DefaultSize="Medium" />
```

When overriding the palette, **always provide both `Light` and `Default` (Dark) theme values**.

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

### Size Variants (Small / Medium)

Pattern: `Simple{Size}{Style}`. `Small` = 32px, `Medium` = 40px (button height).

| Size | Button | Icon Button | ToggleButton |
|---|---|---|---|
| Small | `SimpleSmallFilledButtonStyle`, `SimpleSmallFilledTonalButtonStyle`, `SimpleSmallTextButtonStyle`, `SimpleSmallDangerPrimaryButtonStyle`, `SimpleSmallDangerSubtleButtonStyle` | `SimpleSmallIconButtonStyle`, `SimpleSmallIconButtonNeutralStyle`, `SimpleSmallIconButtonSubtleStyle`, `SimpleSmallIconButtonDangerPrimaryStyle`, `SimpleSmallIconButtonDangerSubtleStyle` | `SimpleSmallToggleButtonStyle` |
| Medium | `SimpleMediumFilledButtonStyle`, `SimpleMediumFilledTonalButtonStyle`, `SimpleMediumTextButtonStyle`, `SimpleMediumDangerPrimaryButtonStyle`, `SimpleMediumDangerSubtleButtonStyle` | `SimpleMediumIconButtonStyle`, `SimpleMediumIconButtonNeutralStyle`, `SimpleMediumIconButtonSubtleStyle`, `SimpleMediumIconButtonDangerPrimaryStyle`, `SimpleMediumIconButtonDangerSubtleStyle` | `SimpleMediumToggleButtonStyle` |

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

1. **Override color palette (full cascade)** — `ColorOverrideSource` / `ColorOverrideDictionary` on `SimpleTheme`. All brushes cascade.
2. **Switch default size variant** — `DefaultSize="Medium"` on `SimpleTheme`. Affects implicit button / icon-button / toggle-button styles only; size-named variants are unaffected.
3. **Override specific brushes (targeted)** — `<SolidColorBrush x:Key="FilledButtonBackground" Color="..." />` in App.xaml.
4. **Override per-control instance (scoped)** — wrap the brush override inside the control's `Resources` block.
5. **Override font family** — `FontOverrideSource` / `FontOverrideDictionary` on `SimpleTheme`, or override `SimpleFontFamily` directly. Default is **Inter**.

Customization precedence (highest → lowest): per-instance `Control.Resources` → `Page.Resources` → app-level overrides → `SimpleTheme` defaults → `SharedColorPalette` / `SharedColors` / `SharedTypography` foundation.

## Related Skills

- [[uno-themes-semantic-colors-brushes]] — **Read this first** for the shared semantic surface (style keys, typography, palette, brushes) common to Simple AND Material. Most styling should use those portable keys; reach for Simple-prefixed styles only when targeting Simple-specific concepts (danger variants, explicit sizes, Expander, etc.).
- [[uno-themes-material]] — Material theme reference. Use when targeting Material instead of (or alongside) Simple.
