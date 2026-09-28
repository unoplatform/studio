---
name: uno-themes
description: "Uno Themes for Uno Platform apps: Material Design 3 via Uno.Material (MaterialTheme), the Simple theme (SimpleTheme, the default of the recommended template preset), and the shared semantic design language they both implement, covering the 33-key color palette and 280 generated brushes (PrimaryBrush, OnSurfaceBrush, ErrorContainerBrush), semantic typography styles (DisplayLarge, BodyMedium, LabelSmall), semantic control style keys (FilledButtonStyle, OutlinedTextBoxStyle, FabStyle), lightweight styling, light/dark mode, ThemeColors palette rebranding and seed colors, DefaultDensity, font overrides, and version migration. Use whenever an Uno app needs colors, brushes, fonts, typography, dark mode, elevation, {ThemeResource} keys, a Button/TextBox/FAB style, 'make it look like Material', 'match our brand colors', the Simple look, or any XAML that hard-codes a Color, FontFamily, or FontSize (it should use semantic brushes and styles instead). Read this before restyling any Uno control."
metadata:
  author: uno-platform
  category: themes
---

# Uno Themes

Uno Themes supplies the look of an Uno Platform app through one of two design themes, Simple (the template default) or Material (MD3), built on a shared semantic layer of style keys, typography keys, palette colors, and generated brushes. Generic agents restyle Uno apps by hard-coding hex colors, font sizes, and copied control templates; that breaks dark mode, theme switching, and brand overrides at once. This skill routes you to the right reference and keeps styling on the semantic keys so the theme does the work.

## Workflow

1. Find the task in the topic map and read the matching `references/*.md` file before writing XAML. Start with `semantic-colors-brushes.md` when the styling should stay portable between Material and Simple.
2. Ground details in the official docs: call `uno_platform_docs_search(...)`, then `uno_platform_docs_fetch(sourcePath="…")` with the `sourcePath` from a result (a relative `.md` path). Never pass a URL, `.html` link, or hand-built path. Simple's pages are `external/uno.themes/doc/simple-getting-started.md` and `simple-controls-styles.md`.
3. Apply the critical rules below to every brush, style, and font you emit, then check the generated XAML against them before finishing.

## Topic map

| Task | Read | Key APIs |
|------|------|----------|
| Pick a brush, a text style, or a control style that works in any theme | `references/semantic-colors-brushes.md` | `PrimaryBrush`/`OnPrimaryBrush` pairs, `BodyMedium`, `FilledButtonStyle` |
| Which brush for text on a surface, an error banner, a snackbar, a CTA | `references/semantic-colors-brushes.md` (Decision Guides) | `OnSurfaceBrush`, `OnSurfaceVariantBrush`, `ErrorContainerBrush`, `SurfaceInverseBrush` |
| Understand palette → brushes → control keys and override precedence | `references/semantic-colors-brushes.md` (Part 3, Part 4) | `*Color` keys, `{Role}{State}Brush`, `FilledButtonBackground` |
| Install Material, set up `MaterialTheme`, Material control styles, FAB, elevation, icons | `references/material.md` | `<UnoFeatures>Material</UnoFeatures>`, `MaterialTheme`, `ut:ControlExtensions.Icon`, `FabStyle` |
| Customize the primary color or fonts for Material, Theme Builder, migration between versions | `references/material.md` (Customization, Migration) | `Colors` (`ThemeColors.OverrideSource` / `PrimarySeed`), `FontOverrideSource`, `material-migration.md` |
| Simple theme (the template default), `SimpleTheme`, grayscale palette, Inter font, density and corner radius | `references/simple.md` | `<UnoFeatures>SimpleTheme</UnoFeatures>`, `SimpleTheme`, `DefaultDensity`, `DefaultCornerRadius` |
| Danger buttons, error inputs, Expander, AutoSuggestBox, ToolTip, PersonPicture, overlay/scrim brushes | `references/simple.md` (Simple-Only Style Keys, Utility Brushes) | `SimpleDangerPrimaryButtonStyle`, `SimpleTextBoxErrorStyle`, `SimpleBackgroundUtilitiesScrimBrush` |
| Change one control's color without touching the palette | `references/semantic-colors-brushes.md` (Part 4) or `references/material.md` (Customization) | Layer 3 keys such as `FilledButtonBackground`, `Control.Resources` |
| Style Uno Toolkit controls or configure `MaterialToolkitTheme` / `SimpleToolkitTheme` / `CupertinoToolkitResources` | the `uno-toolkit` skill (`references/material-theme.md`, `references/lightweight-styling.md`) | `MaterialToolkitTheme`, `SimpleToolkitTheme` |

## Critical rules

- **Never hard-code colors, fonts, or sizes in page XAML.** A literal `#RRGGBB`, `FontSize="14"`, or `FontFamily="Segoe UI"` ignores dark mode and every brand override. Use a semantic brush (`{ThemeResource OnSurfaceBrush}`) and a typography style (`Style="{StaticResource BodyMedium}"`); the theme supplies the concrete values.
- **Brushes use `{ThemeResource}`, styles use `{StaticResource}`.** Brushes must re-resolve when the app switches between Light and Dark; styles never change with the theme and their internal bindings already use `ThemeResource`. Getting this backwards produces text that vanishes in dark mode.
- **Pair every background role with its `On` foreground.** `PrimaryBrush` takes `OnPrimaryBrush`, `ErrorContainerBrush` takes `OnErrorContainerBrush`, `SurfaceBrush` takes `OnSurfaceBrush` (high emphasis) or `OnSurfaceVariantBrush` (medium). Unpaired combinations fail contrast in one of the two modes.
- **Use semantic keys, never theme-prefixed ones, in portable XAML.** Write `FilledButtonStyle` and `DisplayLarge`, not `MaterialFilledButtonStyle` or `SimpleDisplayLarge`. Prefixed keys such as `MaterialBodyMedium` and `SimpleFilledButtonStyle` exist, but only the unprefixed aliases resolve under both themes; the `Simple*` prefix is required only for Simple-only control styles.
- **Restyle through resource keys, not copied templates.** Each control exposes lightweight styling keys (`FilledButtonBackground`, `BodyMediumFontSize`) at app, page, or control scope. Copying a control template freezes it against theme updates and doubles the XAML to maintain.
- **Rebrand at the palette, not brush by brush.** Override the `*Color` keys through the theme's `Colors` property: `<MaterialTheme.Colors>` (or `<SimpleTheme.Colors>`) holding `<ut:ThemeColors xmlns:ut="using:Uno.Themes" OverrideSource="ms-appx:///..." />` (or `OverrideDictionary`). `ColorOverrideSource` / `ColorOverrideDictionary` are obsolete but still work, and the template still emits `ColorOverrideSource`; migrate it when touching that line. Provide both `Light` and `Default` (Dark) values; all 280 brushes and every control follow. The fastest path to a brand palette is a seed: `<ut:ThemeColors PrimarySeed="#0F62FE" />` (also `SecondarySeed`, `TertiarySeed`) generates the whole role set. Override an individual brush only for a one-off exception.
- **Material setup is `<UnoFeatures>Material</UnoFeatures>` plus `<MaterialTheme xmlns="using:Uno.Material" />` in `App.xaml` merged dictionaries.** With Uno Toolkit present, use `MaterialToolkitTheme` instead of `MaterialTheme` + `ToolkitResources`; loading both duplicates resources. The Simple equivalent is `<utus:SimpleToolkitTheme xmlns:utus="using:Uno.Toolkit.UI.Simple" />`, which the recommended preset emits; it derives from `SimpleTheme`, so set `Colors`, `FontOverrideSource`, `DefaultDensity`, and `DefaultCornerRadius` on it and never add a bare `SimpleTheme` next to it.
- **Keep the project's theme; for a new app, follow the template.** `dotnet new unoapp -preset recommended` selects Simple, a lightweight design system with minimal styling (grayscale palette, Inter font, `DefaultDensity` Compact/Regular/Comfy) that also carries controls Material lacks (Expander, AutoSuggestBox, ToolTip, PersonPicture). Choose Material when the brief or design asks for Material Design 3, MD3 color roles or elevation. Both implement the shared semantic layer, so the role brushes (`PrimaryBrush`, `SurfaceBrush`, `OnSurfaceVariantBrush`, ...) resolve under either. Do not switch an existing app's theme unasked, and do not mix theme-specific keys from one into a project running the other. An app created with `-preset blank` uses Fluent and has no Material or Simple theme, so no semantic key resolves there; use WinUI resources, or add a theme only with the user's consent.
- **Spacing and sizes come from design tokens.** `Space*`, `Radius*`, and `ControlHeight*` resource keys (see `external/uno.themes/doc/design-tokens.md`) replace literal margins and heights. On the theme, `DefaultDensity` and `DefaultSpacing` scale only the `Space*` tokens and `DefaultCornerRadius` only the `Radius*` tokens; `ControlHeight*`, `IconSize*`, and `TouchTargetMinSize` are fixed.
- **Version migration is a docs task.** Resource keys, converters, and package names change between major versions; fetch `external/uno.themes/doc/material-migration.md` before upgrading rather than guessing renamed keys. Uno.Themes 8.0 removes `SimpleFontFamily` and the per-weight font keys (the root token is `DefaultFontFamily`) and reworks seed generation (`SeedColorMode`). This skill targets Uno.Themes 8.0; an app still on 7.x lacks `DefaultFontFamily`, `DefaultSpacing`, and `SeedColorMode`.

## Related skills

- `uno-toolkit` for `MaterialToolkitTheme` / `CupertinoToolkitResources` setup and lightweight styling of Toolkit controls (`TabBar`, `CardContentControl`, `NavigationBar`).
- `uno-platform` for project-wide rules and `<UnoFeatures>` configuration.
- `uno-testing` to confirm a restyle renders correctly in both Light and Dark in the running app.
