# Uno Simple Theme: Style Keys and Utility Brushes

The **Simple-only** control styles and brushes, which do not resolve under Material. For portable keys use `references/semantic-colors-brushes.md`; for `SimpleTheme` setup, palette, fonts, and density use `references/simple.md`.

The tables below are a fast lookup; `external/uno.themes/doc/simple-controls-styles.md` is the authoritative key list and wins when they disagree. Docs searches: `uno_platform_docs_search("Uno Simple controls styles danger")`, `uno_platform_docs_search("Uno Simple utility brushes overlay scrim measurement")`.

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
