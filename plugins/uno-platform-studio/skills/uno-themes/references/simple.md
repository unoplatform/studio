# Uno Simple Theme

The Simple theme is a lightweight design system with minimal, essential styling, and the theme `dotnet new unoapp -preset recommended` selects. It shares the semantic surface with Material via the **Semantic Design Language** — see `references/semantic-colors-brushes.md` for the shared layer. This reference covers `SimpleTheme` setup and customization; the Simple-only style keys and utility brushes are in `references/simple-styles.md`.

**Docs searches** (`uno_platform_docs_search`): "Uno Simple theme installation SimpleTheme UnoFeatures", "Uno Themes DefaultDensity design tokens Space Radius ControlHeight", "Uno Themes seed colors PrimarySeed ThemeColors", "Uno Themes ThemeColors OverrideSource palette", "Uno Simple FontOverrideSource Inter font family".

## Critical Rules

- **Simple uses a flat grayscale palette** for all roles except Error (which uses red). Primary, Secondary, Tertiary, Surface, and Outline are achromatic grays. Brand colors come from seeds or a palette override on `SimpleTheme.Colors` (see Configuration).
- **Simple defaults to Inter** for all typography; `CharacterSpacing` is 0 where the M3 scale defines it (Display, Body, Label, Caption); the type scales use `Bold` / `SemiBold` / `Normal` (no `Medium`), but `SimpleButtonFontWeight` and `SimpleToggleButtonFontWeight` are `Medium`. To change the font, see Customization step 5.
- **Sizes come from design tokens, not size-named styles.** `DefaultDensity`, `DefaultSpacing`, and `DefaultCornerRadius` scale the `Space*` and `Radius*` tokens (`RadiusFull` stays 9999); `ControlHeight*`, `IconSize*`, and `TouchTargetMinSize` are fixed. There is no `DefaultSize` and no `SimpleSmall*` / `SimpleMedium*` style family in Uno.Themes 8.0; the one size variant is `SimpleTextBoxSmallStyle`.

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

## Customization Methods

Simple supports five escalating overrides (ordered by scope):

1. **Seed or override the color palette (full cascade)** — seeds or `OverrideSource` / `OverrideDictionary` on `SimpleTheme.Colors`, as above. All brushes cascade.
2. **Density, spacing, corner radius** — `DefaultDensity`, `DefaultSpacing`, `DefaultCornerRadius` on `SimpleTheme`; every control follows through the `Space*` / `Radius*` tokens.
3. **Override specific brushes (targeted)** — `<SolidColorBrush x:Key="FilledButtonBackground" Color="..." />` in App.xaml.
4. **Override per-control instance (scoped)** — wrap the brush override inside the control's `Resources` block.
5. **Override font family** — set `DefaultFontFamily` on the theme (`<SimpleTheme DefaultFontFamily="ms-appx:///Fonts/MyFont.ttf#MyFont" />`); use `FontOverrideSource` / `FontOverrideDictionary` only to override individual font keys. `SimpleFontFamily`, the per-weight `Simple*FontFamily` keys, and the 7.1.1 `TypefacePlain` / `TypefaceBrand` pair no longer exist; an override that still defines them is silently ignored (Uno.Themes 8.0 migration guide). The docs site still serves the 7.x migration page, which recommends `TypefacePlain`; ignore that on 8.0.

Customization precedence (highest → lowest): per-instance `Control.Resources` → `Page.Resources` → app-level overrides → `SimpleTheme` defaults → `SharedColorPalette` / `SharedColors` / `SharedTypography` foundation.

## Related Skills

- `references/semantic-colors-brushes.md` — **Read this first** for the shared semantic surface (style keys, typography, palette, brushes) common to Simple AND Material. Most styling should use those portable keys.
- `references/simple-styles.md` — Simple-only style keys (danger variants, Expander, PersonPicture, etc.) and utility brushes.
- `references/material.md` — Material theme reference. Use when targeting Material instead of (or alongside) Simple.
