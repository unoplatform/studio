---
name: uno-themes
description: "Uno Themes for Uno Platform apps: Material Design 3 via Uno.Material (MaterialTheme), the Simple designer/wireframe theme (SimpleTheme), and the shared semantic design language they both implement, covering the 33-key color palette and ~288 generated brushes (PrimaryBrush, OnSurfaceBrush, ErrorContainerBrush), semantic typography styles (DisplayLarge, BodyMedium, LabelSmall), semantic control style keys (FilledButtonStyle, OutlinedTextBoxStyle, FabStyle), lightweight styling, light/dark mode, ColorOverrideSource rebranding, font overrides, and version migration. Use whenever an Uno app needs colors, brushes, fonts, typography, dark mode, elevation, {ThemeResource} keys, a Button/TextBox/FAB style, 'make it look like Material', 'match our brand colors', a wireframe/prototype look, or any XAML that hard-codes a Color, FontFamily, or FontSize (it should use semantic brushes and styles instead). Read this before restyling any Uno control."
metadata:
  author: uno-platform
  version: "3.0"
  category: themes
---

# Uno Themes

Uno Themes supplies the look of an Uno Platform app through one of two design themes, Material (MD3) or Simple (wireframe), built on a shared semantic layer of style keys, typography keys, palette colors, and generated brushes. Generic agents restyle Uno apps by hard-coding hex colors, font sizes, and copied control templates; that breaks dark mode, theme switching, and brand overrides at once. This skill routes you to the right reference and keeps styling on the semantic keys so the theme does the work.

## Workflow

1. Find the task in the topic map and read the matching `references/*.md` file before writing XAML. Start with `semantic-colors-brushes.md` when the styling should stay portable between Material and Simple.
2. Ground details in the official docs: call `uno_platform_docs_search(...)`, then `uno_platform_docs_fetch(sourcePath="…")` with the `sourcePath` from a result (a relative `.md` path). Never pass a URL, `.html` link, or hand-built path. Simple has no dedicated doc pages; `references/simple.md` is authoritative for it.
3. Apply the critical rules below to every brush, style, and font you emit, then check the generated XAML against them before finishing.

## Topic map

| Task | Read | Key APIs |
|------|------|----------|
| Pick a brush, a text style, or a control style that works in any theme | `references/semantic-colors-brushes.md` | `PrimaryBrush`/`OnPrimaryBrush` pairs, `BodyMedium`, `FilledButtonStyle` |
| Which brush for text on a surface, an error banner, a snackbar, a CTA | `references/semantic-colors-brushes.md` (Decision Guides) | `OnSurfaceBrush`, `OnSurfaceVariantBrush`, `ErrorContainerBrush`, `SurfaceInverseBrush` |
| Understand palette → brushes → control keys and override precedence | `references/semantic-colors-brushes.md` (Part 3, Part 4) | `*Color` keys, `{Role}{State}Brush`, `FilledButtonBackground` |
| Install Material, set up `MaterialTheme`, Material control styles, FAB, elevation, icons | `references/material.md` | `<UnoFeatures>Material</UnoFeatures>`, `MaterialTheme`, `Icons.Icon`, `FabStyle` |
| Customize the primary color or fonts for Material, Theme Builder, migration between versions | `references/material.md` (Customization, Migration) | `ColorOverrideSource`, `FontOverrideSource`, `material-migration.md` |
| Wireframe/prototype look, `SimpleTheme`, grayscale palette, Inter font, size variants | `references/simple.md` | `SimpleTheme`, `DefaultSize`, `SimpleSmall*`/`SimpleMedium*` |
| Danger buttons, error inputs, Expander, AutoSuggestBox, ToolTip, PersonPicture, overlay/scrim brushes | `references/simple.md` (Simple-Only Style Keys, Utility Brushes) | `SimpleDangerPrimaryButtonStyle`, `SimpleTextBoxErrorStyle`, `SimpleBackgroundUtilitiesScrimBrush` |
| Change one control's color without touching the palette | `references/semantic-colors-brushes.md` (Part 4) or `references/material.md` (Customization) | Layer 3 keys such as `FilledButtonBackground`, `Control.Resources` |
| Style Uno Toolkit controls or configure `MaterialToolkitTheme` / `CupertinoToolkitTheme` | the `uno-toolkit` skill (`references/material-theme.md`, `references/lightweight-styling.md`) | `MaterialToolkitTheme` |

## Critical rules

- **Never hard-code colors, fonts, or sizes in page XAML.** A literal `#RRGGBB`, `FontSize="14"`, or `FontFamily="Segoe UI"` ignores dark mode and every brand override. Use a semantic brush (`{ThemeResource OnSurfaceBrush}`) and a typography style (`Style="{StaticResource BodyMedium}"`); the theme supplies the concrete values.
- **Brushes use `{ThemeResource}`, styles use `{StaticResource}`.** Brushes must re-resolve when the app switches between Light and Dark; styles never change with the theme and their internal bindings already use `ThemeResource`. Getting this backwards produces text that vanishes in dark mode.
- **Pair every background role with its `On` foreground.** `PrimaryBrush` takes `OnPrimaryBrush`, `ErrorContainerBrush` takes `OnErrorContainerBrush`, `SurfaceBrush` takes `OnSurfaceBrush` (high emphasis) or `OnSurfaceVariantBrush` (medium). Unpaired combinations fail contrast in one of the two modes.
- **Use semantic keys, never theme-prefixed ones, in portable XAML.** Write `FilledButtonStyle` and `DisplayLarge`, not `MaterialFilledButtonStyle` or `SimpleDisplayLarge`. Typography keys are never prefixed in either theme; the `Simple*` prefix exists only for Simple-only control styles.
- **Restyle through resource keys, not copied templates.** Each control exposes lightweight styling keys (`FilledButtonBackground`, `BodyMediumFontSize`) at app, page, or control scope. Copying a control template freezes it against theme updates and doubles the XAML to maintain.
- **Rebrand at the palette, not brush by brush.** Override the `*Color` keys via `ColorOverrideSource` / `ColorOverrideDictionary` on `MaterialTheme` or `SimpleTheme`, providing both `Light` and `Default` (Dark) values; all ~288 brushes and every control follow. Override an individual brush only for a one-off exception.
- **Material setup is `<UnoFeatures>Material</UnoFeatures>` plus `<MaterialTheme xmlns="using:Uno.Material" />` in `App.xaml` merged dictionaries.** With Uno Toolkit present, use `MaterialToolkitTheme` instead of `MaterialTheme` + `ToolkitResources`; loading both duplicates resources.
- **Choose the theme deliberately.** Material is the default for shipping apps and the only one with MD3 color roles and elevation. Simple is a flat grayscale wireframe theme (Inter font, no `Medium` weight, `DefaultSize` Small/Medium) for prototypes and designer hand-off, and it carries controls Material lacks (Expander, AutoSuggestBox, ToolTip, PersonPicture). Do not mix theme-specific keys from one into a project running the other.
- **Version migration is a docs task.** Resource keys, converters, and package names change between Material major versions; fetch `external/uno.themes/doc/material-migration.md` before upgrading rather than guessing renamed keys.

## Related skills

- `uno-toolkit` for `MaterialToolkitTheme` / `CupertinoToolkitTheme` setup and lightweight styling of Toolkit controls (`TabBar`, `CardContentControl`, `NavigationBar`).
- `uno-platform` for project-wide rules and `<UnoFeatures>` configuration.
- `uno-testing` to confirm a restyle renders correctly in both Light and Dark in the running app.
