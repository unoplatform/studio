---
name: uno-themes-material
description: "Comprehensive reference for the Uno Material theme — Material Design 3 styling for Uno Platform apps. Covers installation, the MD3 color palette and brush system, semantic typography, control styles (buttons, FAB, TextBox), control extensions (icons, elevation), lightweight styling, color/font customization, and version migration."
when_to_use: "Use whenever working with Uno Material theming: setting up MaterialTheme or MaterialToolkitTheme, applying button / FAB / TextBox / typography styles, picking color palette keys, customizing colors or fonts via Theme Builder, applying lightweight styling overrides, adding control extensions (icons, elevation), or migrating between Material versions."
metadata:
  author: uno-platform
  version: "1.0"
  framework: uno-platform
  category: themes
---

# Uno Material — Agent Skill

Consolidated reference for the Uno Material theme. For any deep dive (full key tables, code samples, version-specific gotchas), fetch the authoritative docs via the steps below; the inline tables here cover the most-asked-for facts for fast lookup.

## Workflow

> **Docs lookup:** call `uno_platform_docs_search(...)` first, then `uno_platform_docs_fetch(sourcePath="…")` using the `sourcePath` field from a result (a relative `.md` path; add the result's `anchor` for a section). Never pass a URL, a `.html` link, or a hand-built path.

### Step 1: Pick the relevant topic and search the docs

| Topic | Search query |
|---|---|
| Installation / `MaterialTheme` / `UnoFeatures` | `uno_platform_docs_search("Uno Material installation MaterialTheme UnoFeatures")` |
| Control styles (Button / TextBox / FAB / etc.) | `uno_platform_docs_search("Uno Material controls styles")` |
| Color palette + brushes (MD3) | `uno_platform_docs_search("Uno Material colors brushes MD3 palette")` |
| Typography / type scale | `uno_platform_docs_search("Uno Material typography type scale")` |
| Lightweight styling (per-control overrides) | `uno_platform_docs_search("Uno Material lightweight styling resource keys")` |
| Color customization / Theme Builder | `uno_platform_docs_search("Uno Material color customization Theme Builder DSP")` |
| Font customization | `uno_platform_docs_search("Uno Material font customization font family override")` |
| Control extensions (icons, elevation) | `uno_platform_docs_search("Uno Material control extensions icons elevation")` |
| C# Markup with Material | `uno_platform_docs_search("Uno.Themes.WinUI.Markup C# Markup Material")` |
| Version migration | `uno_platform_docs_search("Uno Material migration v1 v2 v3")` |

### Step 2: Fetch the canonical pages

- **Material Getting Started**: `external/uno.themes/doc/material-getting-started.md`
- **Material Toolkit Getting Started**: `external/uno.toolkit.ui/doc/material-getting-started.md` (use `MaterialToolkitTheme` for Toolkit+Material)
- **Material Controls Styles**: `external/uno.themes/doc/material-controls-styles.md`
- **Lightweight Styling**: `external/uno.themes/doc/lightweight-styling.md`
- **Material Migration**: `external/uno.themes/doc/material-migration.md`

```
uno_platform_docs_fetch(sourcePath="…")  # the sourcePath field from a search result above; never a URL/.html
```

## Critical Rules

- **Brushes use `{ThemeResource}`, NOT `{StaticResource}`** — so the bound color follows the active theme (Light / Dark) at runtime. `<TextBlock Foreground="{ThemeResource PrimaryBrush}" />`.
- **Styles use `{StaticResource}`** — control styles don't change with the theme; their internal bindings already use ThemeResource for brushes.
- **Use `MaterialToolkitTheme` or `MaterialTheme`, not both** — when using both Toolkit + Material, `MaterialToolkitTheme` replaces `MaterialTheme + ToolkitResources`. Mixing them double-loads resources.

## Installation (quick reference)

In `.csproj`:
```xml
<UnoFeatures>Material</UnoFeatures>             <!-- or Material;Toolkit -->
```

In `App.xaml`:
```xml
<ResourceDictionary.MergedDictionaries>
    <MaterialTheme xmlns="using:Uno.Material" />
</ResourceDictionary.MergedDictionaries>
```

Use `<MaterialToolkitTheme xmlns="using:Uno.Toolkit.Material" />` if also using Uno Toolkit.

## Control Style Keys (quick reference)

### Button

| Style key | Use for |
|---|---|
| `FilledButtonStyle` | **Default**. Primary actions ("Save", "Submit") |
| `ElevatedButtonStyle` | Primary action with elevation/shadow |
| `FilledTonalButtonStyle` | Secondary action; softer than Filled |
| `OutlinedButtonStyle` | Secondary action with border |
| `TextButtonStyle` | Tertiary / low-emphasis action |
| `IconButtonStyle` | Icon-only action (pair with `Icons.Icon` attached property) |

### FAB (FloatingActionButton; applied to `Button`)

| Style key family | Sizes |
|---|---|
| `FabStyle` / `SmallFabStyle` / `LargeFabStyle` | Primary FAB |
| `SecondaryFabStyle` / `SecondarySmallFabStyle` / `SecondaryLargeFabStyle` | Secondary color |
| `TertiaryFabStyle` / `TertiarySmallFabStyle` / `TertiaryLargeFabStyle` | Tertiary color |
| `SurfaceFabStyle` / `SurfaceSmallFabStyle` / `SurfaceLargeFabStyle` | Surface color |

### TextBox / PasswordBox

| Style key | Notes |
|---|---|
| `FilledTextBoxStyle` | **Material default** for TextBox |
| `OutlinedTextBoxStyle` | Outlined variant |
| `FilledPasswordBoxStyle` | **Material default** for PasswordBox |
| `OutlinedPasswordBoxStyle` | Outlined variant |

## Color Palette Keys (Layer 1)

Material follows the MD3 role system. 32 color keys with Light/Dark variants. Most-used role families:

- **Primary**: `PrimaryColor`, `OnPrimaryColor`, `PrimaryContainerColor`, `OnPrimaryContainerColor`, `PrimaryInverseColor`
- **Secondary**: `SecondaryColor`, `OnSecondaryColor`, `SecondaryContainerColor`, `OnSecondaryContainerColor`
- **Tertiary**: `TertiaryColor`, `OnTertiaryColor`, `TertiaryContainerColor`, `OnTertiaryContainerColor`
- **Error**: `ErrorColor`, `OnErrorColor`, `ErrorContainerColor`, `OnErrorContainerColor`
- **Surface**: `SurfaceColor`, `OnSurfaceColor`, `SurfaceVariantColor`, `OnSurfaceVariantColor`, `SurfaceInverseColor`, `OnSurfaceInverseColor`, `SurfaceTintColor`
- **Background / Outline / Shadow**: `BackgroundColor`, `OnBackgroundColor`, `OutlineColor`, `OutlineVariantColor`, `ShadowColor`

**Pairing rule**: every background role has a designated foreground role. `PrimaryBrush` ↔ `OnPrimaryBrush`. `ErrorContainerBrush` ↔ `OnErrorContainerBrush`. Don't mix unpaired colors. (See `uno-themes-semantic-colors-brushes` for the shared semantic surface common to Material + Simple.)

## Brush System (Layer 2)

Each color key generates ~9 brush variants with opacity tokens. Pattern: `{ColorRole}{StateVariant}Brush`.

| Suffix | Opacity | Purpose |
|---|---|---|
| *(none)* | 1.0 | Rest state |
| `Hover` | 0.08 | Pointer hover layer |
| `Focused` | 0.12 | Keyboard focus layer |
| `Pressed` | 0.12 | Active press layer |
| `Dragged` | 0.16 | Drag operation layer |
| `Selected` | 0.08 | Selected item layer |
| `Medium` | 0.64 | Medium-emphasis text/icons |
| `Low` | 0.32 | Low-emphasis text, placeholders |
| `Disabled` | 0.12 | Disabled state |

Total: ~288 brushes generated from the palette + opacity tokens.

## Typography (Layer)

M3 type scale, used as `TextBlock` styles. **Never theme-prefix typography keys** — write `DisplayLarge`, not `MaterialDisplayLarge`.

| Family | Keys |
|---|---|
| Display | `DisplayLarge`, `DisplayMedium`, `DisplaySmall` |
| Headline | `HeadlineLarge`, `HeadlineMedium`, `HeadlineSmall` |
| Title | `TitleLarge`, `TitleMedium`, `TitleSmall` |
| Body | `BodyLarge`, `BodyMedium`, `BodySmall` |
| Label | `LabelLarge`, `LabelMedium`, `LabelSmall`, `LabelExtraSmall` |
| Caption | `CaptionLarge`, `CaptionMedium`, `CaptionSmall` |

Each style exposes `{StyleKey}FontFamily`, `FontSize`, `FontWeight` resource keys for lightweight styling overrides.

## Control Extensions

Material ships attached properties for icon, elevation, and toggle-content patterns (namespace `using:Uno.Material.Controls`):

- **`Icons.Icon`** — adds an icon to a `Button` (esp. `IconButtonStyle`).
- **Elevation extensions** — apply MD3 elevation shadows; layered with `ShadowContainer` for richer effects.
- **Alternate content** — display different content based on `ToggleButton.IsChecked`.

## Customization

Three escalating scopes:

1. **Palette override (full cascade)** — override `*Color` keys via `ColorOverrideSource` / `ColorOverrideDictionary` on `MaterialTheme`. All ~288 brushes and controls update automatically. Generate the palette XAML via **Material Theme Builder** (DSP format).
2. **Specific brush override (targeted)** — drop a `<SolidColorBrush x:Key="FilledButtonBackground" Color="..." />` into App.xaml or scoped resources.
3. **Per-instance override (scoped)** — wrap the override in the control's `Resources` block.

Font override: set `FontOverrideSource` / `FontOverrideDictionary` on `MaterialTheme`, or override individual `*FontFamily` keys.

## C# Markup

Use `Uno.Themes.WinUI.Markup` for fluent C# styling — `.MaterialFilledButtonStyle()`, `.MaterialOutlinedTextBoxStyle()`, etc. Resource keys exposed as constants under `MaterialThemeResources`.

```csharp
new Button().Content("Save").MaterialFilledButtonStyle()
```

## Migration

Between major versions, resource keys, converters, and style names can change. Fetch the migration page on upgrade:

```
uno_platform_docs_fetch(sourcePath="external/uno.themes/doc/material-migration.md")
```

Key checks: renamed resource keys, removed converters, NuGet package renames, `UnoFeatures` flag changes.

## Related Skills

- [[uno-themes-semantic-colors-brushes]] — Shared semantic design language (style keys + typography + colors that work across Material AND Simple themes). Read this first if styling needs to be portable between themes.
- [[uno-themes-simple]] — Simple theme specifics. Use when targeting Simple (designer/wireframe look) or its theme-specific styles (danger buttons, size variants, utility brushes, PersonPicture, etc.).
- [[uno-toolkit-material-theme]] — `MaterialToolkitTheme` setup for Toolkit + Material.
