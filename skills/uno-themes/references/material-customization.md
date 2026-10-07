# Uno Material: Customization and Migration

Rebranding the Uno Material theme (palette, individual brushes, fonts) and moving between Uno.Themes versions. For installation and control styles use `references/material.md`; the palette, brush, and typography keys themselves are in `references/semantic-colors-brushes.md`.

**Docs searches** (`uno_platform_docs_search`): "Uno Material color customization Theme Builder DSP", "Uno Material font customization font family override", "Uno Material lightweight styling resource keys", "Uno Material migration v1 v2 v3".

## Customization

Three escalating scopes:

1. **Palette override (full cascade)** — override `*Color` keys through `MaterialTheme.Colors`: `<MaterialTheme.Colors><ut:ThemeColors xmlns:ut="using:Uno.Themes" OverrideSource="ms-appx:///Styles/ColorPaletteOverride.xaml" /></MaterialTheme.Colors>` (or `OverrideDictionary`). `ColorOverrideSource` / `ColorOverrideDictionary` on the theme are obsolete but still work; the template still emits `ColorOverrideSource`, so migrate it when touching that line. All 280 brushes and controls update automatically. Generate the palette XAML via **Material Theme Builder** (DSP format), or skip the file and set seeds: `<ut:ThemeColors PrimarySeed="#0F62FE" SecondarySeed="..." TertiarySeed="..." />` (see `external/uno.themes/doc/seed-colors.md`).
2. **Specific brush override (targeted)** — drop a `<SolidColorBrush x:Key="FilledButtonBackground" Color="..." />` into App.xaml or scoped resources.
3. **Per-instance override (scoped)** — wrap the override in the control's `Resources` block.

Font override: set `DefaultFontFamily` on the theme (`<MaterialTheme DefaultFontFamily="ms-appx:///Fonts/MyFont.ttf#MyFont" />`, a variable font or one with a font manifest); it generates the root token and every type-scale `*FontFamily` key. Use `FontOverrideSource` / `FontOverrideDictionary` only to override individual font keys. The `MaterialRegularFontFamily` / `MaterialMediumFontFamily` / `MaterialLightFontFamily` keys still exist but are legacy, and the 7.1.1 `TypefacePlain` / `TypefaceBrand` pair the docs site may still show is removed in 8.0 (an override that defines them is silently ignored).

## Migration

Between major versions, resource keys, converters, and style names can change. Fetch the migration page on upgrade:

```
uno_platform_docs_fetch(sourcePath="external/uno.themes/doc/material-migration.md")
```

Key checks: renamed resource keys, removed converters, NuGet package renames, `UnoFeatures` flag changes. Uno.Themes 8.0 removes `SimpleFontFamily` and the per-weight font keys (root token `DefaultFontFamily`) and reworks seed generation (`SeedColorMode`).
