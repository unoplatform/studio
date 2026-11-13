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
- **Colours**: semantic brushes (`PrimaryBrush`, `OnPrimaryBrush`, `SurfaceBrush`, `OnSurfaceBrush`, `SurfaceVariantBrush`, `OnSurfaceVariantBrush`, `BackgroundBrush`, `OnBackgroundBrush`, …). Load `uno-themes` and read `references/semantic-colors-brushes.md` for the full set.
- **Gaps**: the mapping tables in the semantic styles doc mark keys one theme does not provide (for example, Simple has no `CommandBar` or media transport style). Do not rely on a gap-listed key in code that must work under both themes.

Read `uno-themes`' `references/simple.md` or `references/material.md` for theme-specific setup, never for page-level styling.

## Brush and pairing rules

- Brushes use `{ThemeResource}` so they follow light and dark switching. Styles and non-brush constants use `{StaticResource}`.
- Every foreground names the background it sits on. Pair each background role with its "On" role: `PrimaryBrush` with `OnPrimaryBrush`, `PrimaryContainerBrush` with `OnPrimaryContainerBrush`, `SurfaceBrush` or `SurfaceVariantBrush` with `OnSurfaceBrush` or `OnSurfaceVariantBrush`, `BackgroundBrush` with `OnBackgroundBrush`, `SurfaceInverseBrush` with `OnSurfaceInverseBrush`. An `On*Container` brush used as text with no container behind it is a pairing error.
- The page root sits on `BackgroundBrush`.
- No colour literals (`#RRGGBB`) in page XAML. A brand colour that the palette cannot express is declared once, in the theme's override dictionary, with both a `Light` and a `Default` (dark) value and its paired foreground.
- No `FontSize`, `FontFamily` or copied `ControlTemplate` in page XAML either. When the design gives its own sizes, typeface, radii or spacing, [design-values.md](design-values.md) says where each goes.
- No value converter that returns a `Brush` or looks one up from `Application.Current.Resources`: the lookup is frozen at bind time and misses theme changes. Use a `VisualState` with `{ThemeResource}` setters driven by the bound value.
- A photo-overlay or hero treatment is not reused as text over a page ground that changes with the theme.
- Icon-only buttons: do not set an explicit `Foreground` on the glyph; the button style supplies the paired colour for every state.

## Brand colour and shape

Recolour through the theme's palette, not brush by brush. For one or two brand colours, use the [seed colour palette](https://platform.uno/docs/articles/external/uno.themes/doc/seed-colors.html) (`PrimarySeed`, optionally `SecondarySeed` and `TertiarySeed` on the theme's `ThemeColors`): it regenerates the whole palette for both themes, and the light Primary is the seed verbatim. When the design gives exact light and dark values for the roles, override all of them through `ThemeColors.OverrideDictionary` instead, since a seed puts generated tones on screen that the design does not list; overriding a single `*Color` alone breaks its generated "On" and container pairings. A brand that changes at runtime is a new override dictionary or seed assigned on the theme, as the `uno-themes` skill describes. Shape and density are set on the theme element, not as `CornerRadius` literals on controls.

**A pill's radius is half its height, never 999.** A design written for the web gives pills, chips and badges a radius such as `999px` or `9999px`, which CSS shrinks evenly to half the height. WinUI, and Uno with it, fits each side's corners on their own instead, so the same value turns anything wider than it is tall into an ellipse. Give the pill a fixed height and declare its radius once, as half of it: a 32-pixel chip takes `16`. If the height can change, with text scaling for example, set the radius from `SizeChanged`. `RadiusFull` is 9999 too: it makes a square element, such as an icon button, a circle, and is not a pill radius.

**Check the attribute exists in the project's resolved version before using it.** Docs and examples track the newest Uno.Themes. For example, Uno.Themes 7.1.1, which Uno.Sdk 6.7.30 pins, has `PrimarySeed` but no `SeedColorMode`; Uno.Themes 9.0.0-dev.42, which Uno.Sdk 7.0.0-dev.77 pins, has both. Build after each theme change; a XAML parse error or a missing resource means the attribute or key does not exist in this version. Do not upgrade packages to get it.

## Existing and legacy themes

Inspect `App.xaml` and follow its merged dictionaries to the theme before touching anything. Never add a second theme element or replace the existing one to restyle the app.

A theme initialised before seed support has no `ThemeColors` element and usually points `ColorOverrideSource` at a colour dictionary. Do not retrofit seeds. Edit the dictionary that `ColorOverrideSource` actually references, keep its URI, and update both the `Light` and `Default` entries plus every paired key you touch.

## Window sizes

A desktop window is resizable and a tablet is rotated or split, so every page has a narrow layout, whatever sizes the brief's mock-ups show (rule 14 of the skill). The wide layout follows the design; the narrow one is decided per page type, before the page's XAML is written:

| Page | Wide | Narrow |
|---|---|---|
| Shell | Navigation pane open beside the content | The pane collapses to its menu button, or the shell switches to a bottom `TabBar`. A `NavigationView` does the first by itself when `PaneDisplayMode` stays `Auto` |
| List with a detail pane beside it | Two columns | The list alone; the selected record's pane covers it, with a close or back action that clears the selection |
| Dashboard, board or card grid | Columns side by side | One column, stacked in reading order; a board's columns scroll sideways |
| Form | Two or more columns of fields | One column; Save and Cancel stay on screen |
| Table-like list | A column per value | Two-line rows: the name, then the secondary values; the least important columns are hidden |
| Page header and command bar | Title, search and every action in one row | Search on its own row; a record page keeps Back, Save and Discard and moves the rest into a More menu (`MenuFlyout`), on every record page |
| Tab bar or filter bar | One row | Its items share the width, wrap to a second row or become one drop-down; no fixed `Width` or `MinWidth` on the bar and no sideways scroll |
| Title and figures | One line | A title wraps or trims with an ellipsis; a row of figures wraps to a second row |

Write it in XAML with the page: `{utu:Responsive}` values, a `ResponsiveView` or visual states. Load `uno-toolkit` and read `references/responsive.md` before the first page's XAML; it has the breakpoints and the list-with-pane pattern. No fixed `Width` on a page root, a pane or a card that a 400-wide window cannot hold: use star columns, `MaxWidth` and a `MinWidth` of 320 or less. The same limit holds inside a dialog, a picker page and a form block, and for the item size of a wrapping panel (`utu:FlexPanel.Basis`, `FlexMinWidth`): a field group that asks for 480 pushes a 400-wide form off screen. A flyout and a tab bar are held to it too. Read each narrow screenshot before accepting it: text cut off at an edge, a tab label missing its end or a button out of reach is a defect.

## Visual acceptance

Apply these to every page the change touches, in each theme the app supports.

**Blocking: tool-assisted, must pass or be reported as failed or not run**

| Check | How | Pass |
|---|---|---|
| Text contrast | Measure foreground against its actual background, from the rendered screenshot or the resolved colour values, in each supported theme, for changed active states (normal, selected, error) | 4.5:1 for body text, 3:1 for large text (18pt, or 14pt bold) and meaningful icons. Inactive controls are exempt from these WCAG thresholds; review their legibility separately |
| Screenshot pairs | One screenshot per changed page in each supported theme, at a narrow window (about 400 wide) and a wide one. A desktop app is not exempt from the narrow one: resize the window with the App MCP (the `uno-testing` skill) | Nothing clipped, truncated or overflowing; every action visible and reachable; no horizontal scrolling of a whole page |
| No colour literals | Search the changed page XAML for `#` colour values | None, except in the theme's override dictionary |
| No size or template literals | Search the changed page XAML for `FontSize=`, `FontFamily=` and `<ControlTemplate` | None, or each one listed in the report with its reason |

Record how each measurement was taken. The thresholds and inactive-control exceptions follow WCAG [text contrast](https://www.w3.org/WAI/WCAG22/Understanding/contrast-minimum.html) and [non-text contrast](https://www.w3.org/WAI/WCAG22/Understanding/non-text-contrast.html). A contrast calculation on a solid surface is a check; it is not an accessibility certification, and gradients or photos need a human look.

**Advisory: reviewer judgement, reported but not blocking**

- Visual hierarchy: one clear primary action per screen, headings that read in order.
- Spacing and alignment consistent with the rest of the app.
- Fidelity to the supplied design, with departures explained.

Capture screenshots with the Uno App MCP (the `uno-testing` skill) or other available UI automation. Without screenshot tooling, report the screenshot checks as not run; source colour-literal searches and contrast calculations from resolved colours can still run. Report their evidence separately.
