# Uno Material

Reference for installing the Uno Material theme and using its control styles and control extensions. Material follows the MD3 role system; its palette keys (33 colors with Light/Dark variants), the 280 generated brushes, and the M3 type scale are the shared semantic keys in `references/semantic-colors-brushes.md`. Palette, brush, and font customization and version migration are in `references/material-customization.md`.

**Docs searches** (`uno_platform_docs_search`): "Uno Material installation MaterialTheme UnoFeatures", "Uno Material controls styles", "Uno Material control extensions icons elevation", "Uno.Themes.WinUI.Markup C# Markup Material". The inline tables below are a fast lookup; the docs win when they disagree.

## Critical Rules

- **Use `MaterialToolkitTheme` or `MaterialTheme`, not both** — when using both Toolkit + Material, `MaterialToolkitTheme` replaces `MaterialTheme + ToolkitResources`. Mixing them double-loads resources. The Simple counterpart is `SimpleToolkitTheme` (see `references/simple.md`).

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

Use `<MaterialToolkitTheme xmlns="using:Uno.Toolkit.UI.Material" />` if also using Uno Toolkit.

## Control Style Keys (quick reference)

### Button

| Style key | Use for |
|---|---|
| `FilledButtonStyle` | **Default**. Primary actions ("Save", "Submit") |
| `ElevatedButtonStyle` | Primary action with elevation/shadow |
| `FilledTonalButtonStyle` | Secondary action; softer than Filled |
| `OutlinedButtonStyle` | Secondary action with border |
| `TextButtonStyle` | Tertiary / low-emphasis action |
| `IconButtonStyle` | Icon-only action (pair with the `ut:ControlExtensions.Icon` attached property) |

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
| `OutlinedTextBoxStyle` | **Material implicit default** for TextBox |
| `FilledTextBoxStyle` | Filled variant |
| `OutlinedPasswordBoxStyle` | **Material implicit default** for PasswordBox |
| `FilledPasswordBoxStyle` | Filled variant |

## Control Extensions

Uno Themes ships attached properties (`ControlExtensions`, `xmlns:ut="using:Uno.Themes"`) that the Material templates consume:

- **`ut:ControlExtensions.Icon`** — adds an icon to a `Button` (esp. `IconButtonStyle`), `TextBox`, `ComboBox`, `PasswordBox`; Material renders it as the leading icon. `ut:ControlExtensions.LeadingIcon` / `TrailingIcon` are consumed only by Simple's `Button` and `ToggleButton` templates.
- **`ut:ControlExtensions.Elevation`** — MD3 elevation, applied by `ElevatedButtonStyle`.
- **`ut:ControlExtensions.AlternateContent`** — content shown when `ToggleButton.IsChecked` is true.

## C# Markup

Use `Uno.Themes.WinUI.Markup`: styles are `StaticResourceKey<Style>` values under `Uno.Themes.Markup.Theme.<Control>.Styles`, and lightweight keys under `Theme.<Control>.Resources`.

```csharp
using Uno.Themes.Markup;

new Button().Content("Save").Style(Theme.Button.Styles.Filled)
// e.g. Theme.Button.Resources.Filled.Background.Default for a resource key
```

## Related Skills

- `references/semantic-colors-brushes.md` — Shared semantic design language (style keys + typography + colors that work across Material AND Simple themes). Read this first if styling needs to be portable between themes.
- `references/material-customization.md` — palette, brush, and font overrides, Theme Builder, migration between versions.
- `references/simple.md` — Simple theme setup and customization; `references/simple-styles.md` — its theme-specific styles (danger buttons, utility brushes, PersonPicture, etc.). Use when targeting Simple (the template default).
- the `uno-toolkit` skill (`references/material-theme.md`) — `MaterialToolkitTheme` setup for Toolkit + Material.
