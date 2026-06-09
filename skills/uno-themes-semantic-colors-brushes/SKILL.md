---
name: uno-themes-semantic-colors-brushes
description: "Comprehensive reference for the Uno Themes Semantic Design Language. Covers semantic control styles, typography, 33 color palette keys, ~288 generated brushes, opacity/state system, color pairing rules, and theme customization."
when_to_use: "Use when styling apps with Uno Themes, choosing correct semantic style keys, using typography resources, selecting color roles, customizing the color palette, or understanding the relationship between colors, brushes, styles, and interactive states."
metadata:
  author: custom
  version: "2.7"
  framework: uno-platform
  category: themes
---

# Uno Themes Semantic Design Language

This skill is the definitive reference for the **Semantic Design Language** in Uno Themes. The semantic system is **design-theme agnostic** -- the same style keys, typography keys, color keys, and brush keys are shared across every design-theme implementation. Each implementation supplies its own concrete values and may not implement every key, but the semantic names are shared. Consult the design-theme-specific skills for concrete styles, palettes, and theme-only extensions.

---

## Part 1: Semantic Control Styles

### How It Works

Each design-theme's `_Resources.xaml` defines `<StaticResource>` aliases that map the shared semantic key to the theme's concrete style (e.g. `FilledButtonStyle` -> `{ThemePrefix}FilledButtonStyle`).

Use semantic keys in XAML for portable, theme-agnostic styling:

```xml
<Button Style="{StaticResource FilledButtonStyle}" Content="Save" />
```

### Complete Style Key Reference

| Semantic Key | Control | Default | Notes |
|---|---|---|---|
| `FilledButtonStyle` | `Button` | Yes | |
| `ElevatedButtonStyle` | `Button` | | Not implemented by every design-theme |
| `FilledTonalButtonStyle` | `Button` | | |
| `OutlinedButtonStyle` | `Button` | | Not implemented by every design-theme |
| `TextButtonStyle` | `Button` | | |
| `IconButtonStyle` | `Button` | | |
| `TextToggleButtonStyle` | `ToggleButton` | | |
| `IconToggleButtonStyle` | `ToggleButton` | Yes | Default implicit for ToggleButton |
| `FilledTextBoxStyle` | `TextBox` | | |
| `OutlinedTextBoxStyle` | `TextBox` | | Implicit default in some design-themes |
| `FilledPasswordBoxStyle` | `PasswordBox` | | |
| `OutlinedPasswordBoxStyle` | `PasswordBox` | | Implicit default in some design-themes |
| `ComboBoxStyle` | `ComboBox` | Yes | |
| `ComboBoxItemStyle` | `ComboBoxItem` | | |
| `CheckBoxStyle` | `CheckBox` | Yes | |
| `RadioButtonStyle` | `RadioButton` | Yes | |
| `ToggleSwitchStyle` | `ToggleSwitch` | Yes | |
| `SliderStyle` | `Slider` | Yes | |
| `HyperlinkButtonStyle` | `HyperlinkButton` | Yes | |
| `SecondaryHyperlinkButtonStyle` | `HyperlinkButton` | | |
| `ListViewStyle` | `ListView` | Yes | |
| `ListViewItemStyle` | `ListViewItem` | Yes | |
| `ContentDialogStyle` | `ContentDialog` | Yes | |
| `CommandBarStyle` | `CommandBar` | Yes | Not implemented by every design-theme |
| `AppBarButtonStyle` | `AppBarButton` | Yes | |
| `NavigationViewStyle` | `NavigationView` | Yes | |
| `NavigationViewItemStyle` | `NavigationViewItem` | Yes | |
| `CalendarViewStyle` | `CalendarView` | Yes | |
| `CalendarDatePickerStyle` | `CalendarDatePicker` | Yes | |
| `DatePickerStyle` | `DatePicker` | Yes | |
| `DatePickerFlyoutPresenterStyle` | `DatePickerFlyoutPresenter` | Yes | Not implemented by every design-theme |
| `MediaTransportControlsStyle` | `MediaTransportControls` | Yes | Not implemented by every design-theme |
| `ProgressBarStyle` | `ProgressBar` | Yes | |
| `ProgressRingStyle` | `ProgressRing` | Yes | |
| `PipsPagerStyle` | `PipsPager` | Yes | |
| `RatingControlStyle` | `RatingControl` | Yes | |
| `FlyoutPresenterStyle` | `FlyoutPresenter` | Yes | |
| `MenuFlyoutPresenterStyle` | `MenuFlyoutPresenter` | Yes | |
| `MenuFlyoutItemStyle` | `MenuFlyoutItem` | Yes | |
| `MenuFlyoutSeparatorStyle` | `MenuFlyoutSeparator` | Yes | |
| `MenuFlyoutSubItemStyle` | `MenuFlyoutSubItem` | Yes | |
| `ToggleMenuFlyoutItemStyle` | `ToggleMenuFlyoutItem` | Yes | |
| `RadioMenuFlyoutItemStyle` | `RadioMenuFlyoutItem` | Yes | |

#### FAB Styles

| Semantic Key | Control | Notes |
|---|---|---|
| `FabStyle` | `Button` | Primary FAB |
| `SmallFabStyle` | `Button` | |
| `LargeFabStyle` | `Button` | |
| `SecondaryFabStyle` | `Button` | |
| `SecondarySmallFabStyle` | `Button` | |
| `SecondaryLargeFabStyle` | `Button` | |
| `TertiaryFabStyle` | `Button` | |
| `TertiarySmallFabStyle` | `Button` | |
| `TertiaryLargeFabStyle` | `Button` | |
| `SurfaceFabStyle` | `Button` | |
| `SurfaceSmallFabStyle` | `Button` | |
| `SurfaceLargeFabStyle` | `Button` | |

> **Note:** Individual design-themes may also expose additional theme-prefixed styles (e.g. danger variants, size variants, controls without a cross-theme equivalent) that are **not** part of the shared semantic surface. See the design-theme-specific skills for those.

---

## Part 2: Semantic Typography

Each design-theme provides semantic typography keys based on the M3 type scale. Concrete font families, sizes, and weights are supplied by the active design-theme's `Typography.xaml`; the semantic key names are shared.

### Typography Style Keys

Use these as TextBlock styles:

```xml
<TextBlock Style="{StaticResource DisplayLarge}" Text="Hero" />
<TextBlock Style="{StaticResource BodyMedium}" Text="Body text" />
```

### Font Resource Keys

Each typography style exposes individual font properties for lightweight styling:

| Style Key | Font Resource Keys |
|---|---|
| `DisplayLarge` | `DisplayLargeFontFamily`, `DisplayLargeFontSize`, `DisplayLargeFontWeight`, `DisplayLargeCharacterSpacing` |
| `DisplayMedium` | `DisplayMediumFontFamily`, `DisplayMediumFontSize`, `DisplayMediumFontWeight` |
| `DisplaySmall` | `DisplaySmallFontFamily`, `DisplaySmallFontSize`, `DisplaySmallFontWeight` |
| `HeadlineLarge` | `HeadlineLargeFontFamily`, `HeadlineLargeFontSize`, `HeadlineLargeFontWeight` |
| `HeadlineMedium` | `HeadlineMediumFontFamily`, `HeadlineMediumFontSize`, `HeadlineMediumFontWeight` |
| `HeadlineSmall` | `HeadlineSmallFontFamily`, `HeadlineSmallFontSize`, `HeadlineSmallFontWeight` |
| `TitleLarge` | `TitleLargeFontFamily`, `TitleLargeFontSize`, `TitleLargeFontWeight` |
| `TitleMedium` | `TitleMediumFontFamily`, `TitleMediumFontSize`, `TitleMediumFontWeight` |
| `TitleSmall` | `TitleSmallFontFamily`, `TitleSmallFontSize`, `TitleSmallFontWeight` |
| `BodyLarge` | `BodyLargeFontFamily`, `BodyLargeFontSize`, `BodyLargeFontWeight` |
| `BodyMedium` | `BodyMediumFontFamily`, `BodyMediumFontSize`, `BodyMediumFontWeight` |
| `BodySmall` | `BodySmallFontFamily`, `BodySmallFontSize`, `BodySmallFontWeight` |
| `LabelLarge` | `LabelLargeFontFamily`, `LabelLargeFontSize`, `LabelLargeFontWeight` |
| `LabelMedium` | `LabelMediumFontFamily`, `LabelMediumFontSize`, `LabelMediumFontWeight` |
| `LabelSmall` | `LabelSmallFontFamily`, `LabelSmallFontSize`, `LabelSmallFontWeight` |
| `LabelExtraSmall` | `LabelExtraSmallFontFamily`, `LabelExtraSmallFontSize`, `LabelExtraSmallFontWeight` |
| `CaptionLarge` | `CaptionLargeFontFamily`, `CaptionLargeFontSize`, `CaptionLargeFontWeight` |
| `CaptionMedium` | `CaptionMediumFontFamily`, `CaptionMediumFontSize`, `CaptionMediumFontWeight` |
| `CaptionSmall` | `CaptionSmallFontFamily`, `CaptionSmallFontSize`, `CaptionSmallFontWeight` |

### Typography Decision Guide

| Purpose | Style Key |
|---------|-----------|
| Hero / splash text | `DisplayLarge` |
| Page title | `DisplayMedium` or `HeadlineLarge` |
| Section heading | `HeadlineMedium` or `HeadlineSmall` |
| Card / dialog title | `TitleLarge` or `TitleMedium` |
| Button / tab label | `LabelLarge` |
| Primary body text | `BodyLarge` |
| Secondary body text | `BodyMedium` |
| Caption / helper text | `CaptionMedium` or `BodySmall` |
| Badge / small label | `LabelSmall` or `LabelExtraSmall` |

---

## Part 3: Semantic Colors & Brushes

### Architecture: Three Layers

```
Layer 1: Color Palette (SharedColorPalette.xaml)
    33 Color resources with Light/Dark theme variants
           |
           v
Layer 2: Semantic Brushes (SharedColors.xaml)
    ~288 SolidColorBrush resources generated from palette + opacity tokens
           |
           v
Layer 3: Control Lightweight Styling Keys
    Per-control resource keys that reference semantic brushes
    (e.g., FilledButtonBackground -> PrimaryBrush)
```

- **Override Layer 1** to rebrand the entire app (all brushes cascade automatically).
- **Override Layer 2** to change a specific brush without affecting the palette.
- **Override Layer 3** to change a single control's appearance without affecting the brush system.

### The Color Palette (Layer 1)

Defined in `SharedColorPalette.xaml`, these 33 `Color` resources are the foundation.

#### Primary -- Brand Identity & Key Actions

| Color Key | Role | When to Use |
|-----------|------|-------------|
| `PrimaryColor` | Hero brand color | Filled button backgrounds, active toggle states, FAB backgrounds |
| `OnPrimaryColor` | Content on Primary | Text and icons on Primary surfaces |
| `PrimaryContainerColor` | Softer primary fill | Tonal button backgrounds, selected navigation items, chips |
| `OnPrimaryContainerColor` | Content on Primary Container | Text and icons on PrimaryContainer surfaces |
| `PrimaryInverseColor` | Inverted primary accent | Links on inverse surfaces (e.g., dark snackbar) |

#### Legacy Primary Variants

| Color Key | Role |
|-----------|------|
| `PrimaryVariantDarkColor` | Darker primary shade (M2 compat) |
| `PrimaryVariantLightColor` | Lighter primary shade (M2 compat) |

#### Secondary -- Supporting Accents

| Color Key | Role | When to Use |
|-----------|------|-------------|
| `SecondaryColor` | Supporting accent | Filter chips, secondary action buttons |
| `OnSecondaryColor` | Content on Secondary | Text and icons on Secondary surfaces |
| `SecondaryContainerColor` | Softer secondary fill | NavigationView selected item, info banners |
| `OnSecondaryContainerColor` | Content on Secondary Container | Text and icons on SecondaryContainer |

#### Legacy Secondary Variants

| Color Key | Role |
|-----------|------|
| `SecondaryVariantDarkColor` | Darker secondary shade (M2 compat) |
| `SecondaryVariantLightColor` | Lighter secondary shade (M2 compat) |

#### Tertiary -- Contrast & Balance

| Color Key | Role | When to Use |
|-----------|------|-------------|
| `TertiaryColor` | Third accent | Tertiary FABs, accents distinct from Primary and Secondary |
| `OnTertiaryColor` | Content on Tertiary | Text and icons on Tertiary surfaces |
| `TertiaryContainerColor` | Softer tertiary fill | Complementary cards, input fields |
| `OnTertiaryContainerColor` | Content on Tertiary Container | Text and icons on TertiaryContainer |

#### Error -- Validation & Destructive Actions

| Color Key | Role | When to Use |
|-----------|------|-------------|
| `ErrorColor` | Danger/error accent | Error text, destructive button backgrounds |
| `OnErrorColor` | Content on Error | Text and icons on Error surfaces |
| `ErrorContainerColor` | Softer error fill | Error banners, validation backgrounds |
| `OnErrorContainerColor` | Content on Error Container | Text and icons on ErrorContainer |

#### Surface -- Component Backgrounds

| Color Key | Role | When to Use |
|-----------|------|-------------|
| `SurfaceColor` | Default component background | Cards, dialogs, sheets, menus |
| `OnSurfaceColor` | Primary text/icons | Highest-emphasis content: titles, body text |
| `SurfaceVariantColor` | Differentiated surface | ToggleSwitch tracks, Slider tracks, subtle section backgrounds |
| `OnSurfaceVariantColor` | Secondary text/icons | Helper text, captions, lower-emphasis icons |
| `SurfaceInverseColor` | Inverted surface | Snackbars, tooltips |
| `OnSurfaceInverseColor` | Content on inverse surface | Text on snackbars and tooltips |
| `SurfaceTintColor` | Elevation tint | Tint overlay for elevated surfaces |

#### Background, Outline, Shadow

| Color Key | Role |
|-----------|------|
| `BackgroundColor` | Root page background |
| `OnBackgroundColor` | Content on background |
| `OutlineColor` | Interactive boundaries (button borders, input outlines) |
| `OutlineVariantColor` | Decorative boundaries (card edges, dividers) |
| `ShadowColor` | Drop shadow color |

### The Brush System (Layer 2)

Every Color key generates **9 SolidColorBrush variants** at different opacity levels.

#### Brush Naming Pattern: `{ColorRole}{StateVariant}Brush`

| Suffix | Opacity | Purpose |
|--------|---------|---------|
| *(none)* | 1.0 | Default/rest state |
| `Hover` | 0.08 | Pointer hover state layer |
| `Focused` | 0.12 | Keyboard focus state layer |
| `Pressed` | 0.12 | Active press state layer |
| `Dragged` | 0.16 | Drag operation state layer |
| `Selected` | 0.08 | Selected item state layer |
| `Medium` | 0.64 | Medium-emphasis text/icons |
| `Low` | 0.32 | Low-emphasis text, placeholders |
| `Disabled` | 0.12 | Disabled state backgrounds |

**Total: ~288 semantic brushes** across Primary (63), Secondary (54), Tertiary (36), Error (36), Surface (63), Background (18), Outline (18).

### Color Pairing Rules (CRITICAL)

**NEVER mix unpaired colors.** Every background role has a designated "On" foreground role.

| If Background Is... | Then Foreground MUST Be... |
|----------------------|---------------------------|
| `PrimaryBrush` | `OnPrimaryBrush` |
| `PrimaryContainerBrush` | `OnPrimaryContainerBrush` |
| `SecondaryBrush` | `OnSecondaryBrush` |
| `SecondaryContainerBrush` | `OnSecondaryContainerBrush` |
| `TertiaryBrush` | `OnTertiaryBrush` |
| `TertiaryContainerBrush` | `OnTertiaryContainerBrush` |
| `ErrorBrush` | `OnErrorBrush` |
| `ErrorContainerBrush` | `OnErrorContainerBrush` |
| `SurfaceBrush` | `OnSurfaceBrush` or `OnSurfaceVariantBrush` |
| `SurfaceVariantBrush` | `OnSurfaceVariantBrush` |
| `BackgroundBrush` | `OnBackgroundBrush` |
| `SurfaceInverseBrush` | `OnSurfaceInverseBrush` |

### Text Emphasis on Surfaces

| Emphasis | Brush | Use For |
|----------|-------|---------|
| High | `OnSurfaceBrush` | Titles, headings, primary labels |
| Medium | `OnSurfaceVariantBrush` | Body text, secondary labels, captions |
| Low / Disabled | `OnSurfaceLowBrush` | Disabled text, placeholder text |

---

## Part 4: Customization

### Method 1: Override Color Palette (Full Cascade)

Override `*Color` keys in a ResourceDictionary referenced by your theme. All ~288 brushes and controls update automatically.

**Always provide both** `Light` and `Default` (Dark) theme values.

### Method 2: Override Specific Brushes (Targeted)

```xml
<SolidColorBrush x:Key="FilledButtonBackground" Color="#006D3B" />
```

### Method 3: Override Per-Control Instance (Scoped)

```xml
<Button Style="{StaticResource FilledButtonStyle}">
    <Button.Resources>
        <ResourceDictionary>
            <ResourceDictionary.ThemeDictionaries>
                <ResourceDictionary x:Key="Light">
                    <SolidColorBrush x:Key="FilledButtonBackground" Color="#006D3B" />
                </ResourceDictionary>
            </ResourceDictionary.ThemeDictionaries>
        </ResourceDictionary>
    </Button.Resources>
</Button>
```

### Customization Precedence (Highest to Lowest)

1. **Control.Resources** -- per-instance
2. **Page.Resources** -- page-scoped
3. **App-level overrides** -- app-wide
4. **Theme defaults** -- active design-theme
5. **SharedColorPalette / SharedColors / SharedTypography** -- foundation

---

## Decision Guides

### Which Color Role?

| Scenario | Background | Foreground |
|----------|------------|------------|
| Primary CTA ("Submit") | `PrimaryBrush` | `OnPrimaryBrush` |
| Tonal action | `SecondaryContainerBrush` | `OnSecondaryContainerBrush` |
| Outlined button border | `OutlineBrush` | `PrimaryBrush` |
| Text-only action | Transparent | `PrimaryBrush` |
| Destructive ("Delete") | `ErrorBrush` | `OnErrorBrush` |
| Card / Dialog | `SurfaceBrush` | `OnSurfaceBrush` |
| Error banner | `ErrorContainerBrush` | `OnErrorContainerBrush` |
| Snackbar / Tooltip | `SurfaceInverseBrush` | `OnSurfaceInverseBrush` |

### Which Button Style?

| Scenario | Style Key |
|----------|-----------|
| Primary action | `FilledButtonStyle` |
| Secondary action | `FilledTonalButtonStyle` or `OutlinedButtonStyle` |
| Tertiary / low emphasis | `TextButtonStyle` |
| Icon-only action | `IconButtonStyle` |

### XAML Best Practices

- Always use `ThemeResource` (not `StaticResource`) for brushes so they respond to theme changes.
- Always use `StaticResource` for styles (styles don't change with theme).
- Use semantic keys, not theme-prefixed keys, for portable XAML.

```xml
<!-- Correct -->
<Button Style="{StaticResource FilledButtonStyle}"
        Content="Save" />

<!-- Avoid theme-prefixed keys in portable code -->
<!-- <Button Style="{StaticResource {ThemePrefix}FilledButtonStyle}" /> -->
```
