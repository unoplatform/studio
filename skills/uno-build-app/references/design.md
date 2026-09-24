# Design and Visual Acceptance

Functional correctness and visual quality are assessed separately, against the supplied design or the fallback, across the themes and window sizes the change affects. Neither compensates for the other: a working feature that is unreadable in light theme is not done, and neither is a beautiful page whose buttons do nothing.

## Supplied design or fallback

- **A design was supplied** (mockups, a Figma export, a brand guide, a colour file): follow it. Choose theme and controls to fit it. If it is a Material design, the Material theme is a legitimate choice for that app. Label any adaptation you had to make.
- **No design**: the fallback is the Simple theme with the template's Toolkit controls, which is what [new-app.md](new-app.md) scaffolds.
- **Existing app**: keep its theme family and its initialisation, whatever they are. See *Existing and legacy themes* below.

## Bind the semantic layer, not a theme

Uno Themes exposes a [semantic style layer](https://platform.uno/docs/articles/external/uno.themes/doc/semantic-styles.html) shared by Material and Simple. Write XAML against it, so the theme family underneath stays interchangeable:

- **Control styles**: semantic keys such as `FilledButtonStyle`, `OutlinedButtonStyle`, `TextButtonStyle`, `FilledTextBoxStyle`. Never a theme-prefixed key like `MaterialFilledButtonStyle`.
- **Typography**: the shared type-scale keys (`DisplayLarge` … `CaptionSmall`, `TitleMedium`, `BodyMedium`, `LabelLarge`, …) as `TextBlock` styles.
- **Colours**: semantic brushes (`PrimaryBrush`, `OnPrimaryBrush`, `SurfaceBrush`, `OnSurfaceBrush`, `SurfaceVariantBrush`, `OnSurfaceVariantBrush`, `BackgroundBrush`, `OnBackgroundBrush`, …). Load `uno-themes-semantic-colors-brushes` for the full set.
- **Gaps**: the mapping tables in the semantic styles doc mark keys one theme does not provide (for example, Simple has no `CommandBar` or media transport style). Do not rely on a gap-listed key in code that must work under both themes.

Load `uno-themes-simple` or `uno-themes-material` for theme-specific setup, never for page-level styling.

## Brush and pairing rules

- Brushes use `{ThemeResource}` so they follow light and dark switching. Styles and non-brush constants use `{StaticResource}`.
- Every foreground names the background it sits on. Pair each background role with its "On" role: `PrimaryBrush` with `OnPrimaryBrush`, `PrimaryContainerBrush` with `OnPrimaryContainerBrush`, `SurfaceBrush` or `SurfaceVariantBrush` with `OnSurfaceBrush` or `OnSurfaceVariantBrush`, `BackgroundBrush` with `OnBackgroundBrush`, `SurfaceInverseBrush` with `OnSurfaceInverseBrush`. An `On*Container` brush used as text with no container behind it is a pairing error.
- The page root sits on `BackgroundBrush`.
- No colour literals (`#RRGGBB`) in page XAML. A brand colour that the palette cannot express is declared once, in the theme's override dictionary, with both a `Light` and a `Default` (dark) value and its paired foreground.
- No value converter that returns a `Brush` or looks one up from `Application.Current.Resources`: the lookup is frozen at bind time and misses theme changes. Use a `VisualState` with `{ThemeResource}` setters driven by the bound value.
- A photo-overlay or hero treatment is not reused as text over a page ground that changes with the theme.
- Icon-only buttons: do not set an explicit `Foreground` on the glyph; the button style supplies the paired colour for every state.

## Brand colour and shape

Recolour through the theme's generator, not by rewriting palette keys. Setting a single `*Color` override silently breaks its generated "On" and container pairings. The [seed colour palette](https://platform.uno/docs/articles/external/uno.themes/doc/seed-colors.html) (`PrimarySeed`, optionally `SecondarySeed` and `TertiarySeed` on the theme's `ThemeColors`) regenerates the whole palette for both themes. Shape and density are set on the theme element, not as `CornerRadius` literals on controls.

**Check the attribute exists in the project's resolved version before using it.** Docs and examples track the newest Uno.Themes. For example, Uno.Themes 7.1.1, which Uno.Sdk 6.7.30 pins, has `PrimarySeed` but no `SeedColorMode`. Build after each theme change; a XAML parse error or a missing resource means the attribute or key does not exist in this version. Do not upgrade packages to get it.

## Existing and legacy themes

Inspect `App.xaml` and follow its merged dictionaries to the theme before touching anything. Never add a second theme element or replace the existing one to restyle the app.

A theme initialised before seed support has no `ThemeColors` element and usually points `ColorOverrideSource` at a colour dictionary. Do not retrofit seeds. Edit the dictionary that `ColorOverrideSource` actually references, keep its URI, and update both the `Light` and `Default` entries plus every paired key you touch.

## Visual acceptance

Apply these to every page the change touches, in each theme the app supports.

**Blocking: tool-assisted, must pass or be reported as failed or not run**

| Check | How | Pass |
|---|---|---|
| Text contrast | Measure foreground against its actual background, from the rendered screenshot or the resolved colour values, in light and in dark, for every changed state (normal, disabled, selected, error) | 4.5:1 for body text, 3:1 for large text (18pt, or 14pt bold) and for icons that carry meaning |
| Screenshot pairs | One screenshot per changed page in light and dark, at a narrow (phone-width) and a wide (desktop) window size | Nothing clipped, truncated or overflowing; every action visible and reachable |
| No colour literals | Search the changed page XAML for `#` colour values | None, except in the theme's override dictionary |

Record how each measurement was taken. A contrast calculation on a solid surface is a check; it is not an accessibility certification, and gradients or photos need a human look.

**Advisory: reviewer judgement, reported but not blocking**

- Visual hierarchy: one clear primary action per screen, headings that read in order.
- Spacing and alignment consistent with the rest of the app.
- Fidelity to the supplied design, with departures explained.

Screenshots need the Uno App MCP (`uno-testing-ui`). Without it, the blocking checks are reported as not run, never as passed.
