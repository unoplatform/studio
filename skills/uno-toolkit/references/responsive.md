# Uno Toolkit Responsive

**Contents:** [Critical Rules](#critical-rules) · [Key Principles (Stable)](#key-principles-stable) · [A narrow layout for every page](#a-narrow-layout-for-every-page) · [Docs, for what this page does not cover](#docs-for-what-this-page-does-not-cover) · [Related Skills](#related-skills)

## Critical Rules

- **`ResponsiveExtension` must be used as a markup extension** — `{utu:Responsive Narrow=Red, Wide=Blue}` — never as a XAML element (`<utu:ResponsiveExtension .../>`). The XAML engine parses the breakpoint values as strings and performs type conversion through `MarkupExtension.ProvideValue`, which is only available in markup-extension form.
- **The window's width decides layout in XAML, never in code-behind.** A value that changes with width is `{utu:Responsive}`; a layout that changes shape is a `ResponsiveView`, or visual states with an `AdaptiveTrigger`. Do not read `ActualWidth`, `SizeChanged` or `XamlRoot.Size` in a page's `.xaml.cs` to swap panels, columns or templates: the layout rule then lives outside the XAML it changes, and every page ends up with its own breakpoints.

## Key Principles (Stable)

- **ResponsiveExtension** — sets property values per breakpoint (Narrow, Normal, Wide, etc.) via the `{utu:Responsive ...}` markup-extension form
- **ResponsiveView** — swaps entire DataTemplates per breakpoint (used as an element)
- Default breakpoints: Narrowest (150), Narrow (300), Normal (600), Wide (800), Widest (1080)
- Custom breakpoints via `ResponsiveLayout` resource
- XAML namespace: `xmlns:utu="using:Uno.Toolkit.UI"`

## A narrow layout for every page

Every page gets a narrow layout, in a desktop app too: its window is resizable. Write it with the page.

**Two values are enough.** A breakpoint with no value is skipped, and the value used is the one for the largest breakpoint the window's width has reached, or the first one given when it has reached none. So `{utu:Responsive Narrow=..., Wide=...}` gives the `Narrow` value below 800 and the `Wide` value from 800 up, and a 1280-wide design keeps its wide layout. Give the breakpoints once for the app when the design needs others: a `utu:ResponsiveLayout` in `Application.Resources` with `x:Key="DefaultResponsiveLayout"`.

**What changes with the width:**

| Need | Write |
|---|---|
| Side by side when wide, stacked when narrow | `Orientation="{utu:Responsive Narrow=Vertical, Wide=Horizontal}"` on the `StackPanel` or `AutoLayout`; in a `Grid`, `Grid.Row` and `Grid.Column` set the same way |
| Shown only when wide (a secondary column, a caption, a side summary) | `Visibility="{utu:Responsive Narrow=Collapsed, Wide=Visible}"` |
| A different amount of space | `Padding`, `Margin`, `Spacing`, `Width` or `MaxWidth` with `{utu:Responsive}` values |
| A different shape altogether (table rows when wide, two-line rows when narrow) | `utu:ResponsiveView` with a `NarrowTemplate` and a `WideTemplate`, or two item templates switched by visual states |
| The shell's pane | `NavigationView` with `PaneDisplayMode="Auto"`: expanded from 1008, compact from 641, a menu button below. A bottom `TabBar` under a width: the `uno-navigation` skill (`references/responsive-shell.md`) |

**A list with a detail pane beside it.** When narrow, the pane covers the list instead of sitting beside it, so nothing is squeezed. The pane is already hidden while nothing is selected; give it a close action that clears the selection.

```xml
<Grid>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*" />
        <ColumnDefinition Width="{utu:Responsive Narrow=0, Wide=420}" />
    </Grid.ColumnDefinitions>

    <ListView Grid.Column="0" ... />

    <!-- Wide: column 1. Narrow: both columns, over the list. -->
    <Grid Grid.Column="{utu:Responsive Narrow=0, Wide=1}"
          Grid.ColumnSpan="{utu:Responsive Narrow=2, Wide=1}"
          Background="{ThemeResource SurfaceBrush}"
          Visibility="..." />
</Grid>
```

`{utu:Responsive}` on a `ColumnDefinition` works in a page's or control's own XAML. It is not applied in a separate `ResourceDictionary` file or inside a `DataTemplate` (an item, `FeedView` or content template): the app logs `Failed to register ResponsiveExtension` and the column keeps one width. In a template, give the column `Width="Auto"` and put the responsive value on the element in it:

```xml
<TextBlock Grid.Column="1" Width="140" Text="{Binding City}"
           Visibility="{utu:Responsive Narrow=Collapsed, Wide=Visible}" />
```

**No fixed width that a narrow window cannot hold.** A `Width` on a page root, pane, card or text box that is wider than about 320 becomes a `MaxWidth`, a star column or a `{utu:Responsive}` value.

**Check it.** Resize the running app's window to about 400 wide (the `uno-testing` skill: `uno_app_resize`) and take a screenshot of each page: nothing clipped, every action reachable, no page scrolling sideways. Then resize back.

## Docs, for what this page does not cover

Use these when the sections above do not answer the question: search, then fetch only the section you need, with the `anchor` from the search result. A whole page stays in context for the rest of the session.

### Fetch the ResponsiveExtension Documentation

```
uno_platform_docs_search("Uno Toolkit ResponsiveExtension responsive screen size breakpoints property")
```

Primary documentation pages:
- **ResponsiveExtension**: `external/uno.toolkit.ui/doc/helpers/responsive-extension.md`
- **ResponsiveView**: `external/uno.toolkit.ui/doc/controls/ResponsiveView.md`

Fetch ResponsiveExtension:

```
uno_platform_docs_fetch(sourcePath="external/uno.toolkit.ui/doc/helpers/responsive-extension.md", anchor="…")
```

### For ResponsiveView (Template Switching)

Fetch ResponsiveView for swapping entire layouts:

```
uno_platform_docs_fetch(sourcePath="external/uno.toolkit.ui/doc/controls/ResponsiveView.md", anchor="…")
```

### For How-To Examples

```
uno_platform_docs_search("ResponsiveExtension howto walkthrough responsive layout breakpoints")
```

Key page:
- **ResponsiveExtension How-To**: `external/uno.toolkit.ui/doc/helpers/walkthroughs/responsive-extension.howto.md`

## Related Skills

- the `uno-navigation` skill (`references/responsive-shell.md`) — Responsive navigation shells
