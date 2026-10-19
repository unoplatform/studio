# Uno Material: Customization and Migration

Rebranding the Uno Material theme (palette, individual brushes, fonts) and moving between Uno.Themes versions. For installation and control styles use `references/material.md`; the palette, brush, and typography keys themselves are in `references/semantic-colors-brushes.md`.

**Docs searches** (`uno_platform_docs_search`): "Uno Material color customization Theme Builder DSP", "Uno Material font customization font family override", "Uno Material lightweight styling resource keys", "Uno Material migration v1 v2 v3".

## Customization

Three escalating scopes:

1. **Palette override (full cascade)** — override `*Color` keys through `MaterialTheme.Colors`: `<MaterialTheme.Colors><ut:ThemeColors xmlns:ut="using:Uno.Themes" OverrideSource="ms-appx:///Styles/ColorPaletteOverride.xaml" /></MaterialTheme.Colors>` (or `OverrideDictionary`). `ColorOverrideSource` / `ColorOverrideDictionary` on the theme are obsolete but still work; the template still emits `ColorOverrideSource`, so migrate it when touching that line. All 280 brushes and controls update automatically. Generate the palette XAML via **Material Theme Builder** (DSP format), or skip the file and set seeds: `<ut:ThemeColors PrimarySeed="#0F62FE" SecondarySeed="..." TertiarySeed="..." />` (see `external/uno.themes/doc/seed-colors.md`).
2. **Specific brush override (targeted)** — drop a `<SolidColorBrush x:Key="FilledButtonBackground" Color="..." />` into App.xaml or scoped resources.
3. **Per-instance override (scoped)** — wrap the override in the control's `Resources` block.

Font override: point `FontOverrideSource` at a ResourceDictionary (or inline one in `FontOverrideDictionary`, as the template's commented block shows) that redefines `MaterialLightFontFamily`, `MaterialMediumFontFamily` and `MaterialRegularFontFamily`, or the root typeface tokens `TypefacePlain` / `TypefaceBrand` that the type scales derive from. Define the keys under both `Light` and `Default` theme dictionaries. There is no `DefaultFontFamily` property in Uno.Themes 7.1.1.

## Migration

Between major versions, resource keys, converters, and style names can change. Fetch the migration page on upgrade:

```
uno_platform_docs_fetch(sourcePath="external/uno.themes/doc/material-migration.md")
```

Key checks: renamed resource keys, removed converters, NuGet package renames, `UnoFeatures` flag changes. This reference targets Uno.Themes 7.1.1 (Uno.Sdk 6.7); a newer major may rename the font keys and the seed options, so check the migration page before using a key the docs show.
