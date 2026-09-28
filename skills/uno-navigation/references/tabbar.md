# Uno Navigation with TabBar

## Reference Templates

For complete, compilable shell XAML with parameterized placeholders, see:

**`references/shell-tabbar-template.md`** — Contains:
- Icon lookup table (Home, Search, Settings, Profile, etc.)
- SHELL_PAGE_NAME.xaml shell template with TabBar
- Page template (XAML + code-behind)
- Route registration template
- Substitution rules
- Localization template (Resources.resw)

## Workflow

> **Docs lookup:** call `uno_platform_docs_search(...)` first, then `uno_platform_docs_fetch(sourcePath="…")` using the `sourcePath` field from a result (a relative `.md` path; add the result's `anchor` for a section). Never pass a URL, a `.html` link, or a hand-built path.

### Step 1: Fetch the TabBar Navigation Documentation

```
uno_platform_docs_search("Uno Navigation TabBar bottom tab region navigation")
```

Primary documentation pages:
- **Use TabBar (walkthrough)**: `external/uno.extensions/doc/Learn/Navigation/Walkthrough/UseTabBar.md`
- **Define Regions**: `external/uno.extensions/doc/Learn/Navigation/Walkthrough/DefineRegions.md`
- **TabBar Navigation (Chefs recipe, thinner)**: `external/uno.chefs/doc/toolkit/NavigateTabBar.md`

Fetch the TabBar walkthrough:

```
uno_platform_docs_fetch(sourcePath="external/uno.extensions/doc/Learn/Navigation/Walkthrough/UseTabBar.md")
```

### Step 2: For TabBar Control Reference

For TabBar control properties and styling:

```
uno_platform_docs_search("Uno Toolkit TabBar TabBarItem styles badges")
```

See also the `uno-toolkit` skill (`references/tabbar.md`) for the control itself.

### Step 3: For Responsive Navigation

If combining TabBar (mobile) with NavigationView (desktop):

See the `references/responsive-shell.md`.

## Key Principles (Stable)

- TabBar uses region-based navigation similarly to NavigationView
- Each `TabBarItem` gets `uen:Region.Name="RouteName"`
- Parent container and content area need `Region.Attached="True"`
- Apply `BottomTabBarStyle` to the `TabBar` and `BottomTabBarItemStyle` to each `TabBarItem`; these alias keys are defined only by the Material and Simple toolkit themes. On Fluent (the `-preset blank` default) omit the `Style` attributes or the page throws `XamlParseException: Cannot find a Resource with the Name/Key BottomTabBarStyle`; on Cupertino use `CupertinoBottomTabBarStyle`/`CupertinoBottomTabBarItemStyle`. Likewise replace the page `Background="{ThemeResource BackgroundBrush}"` with `ApplicationPageBackgroundThemeBrush` on Fluent or `CupertinoSystemBackgroundBrush` on Cupertino. `MaterialBottomTabBarItemStyle` is the Material-only alias and throws under Simple; see the `uno-toolkit` skill (`references/tabbar.md`, `references/material-theme.md`)
- Requires `Toolkit` in `<UnoFeatures>` along with `Navigation`
- XAML namespaces: `xmlns:uen="using:Uno.Extensions.Navigation.UI"` and `xmlns:utu="using:Uno.Toolkit.UI"`

## Critical Rules

- The root `Grid` with `uen:Region.Attached="True"` is the Page content (the template shows it as the direct child of `Page`); it enables region navigation
- `uen:Region.Navigator="Visibility"` MUST be on the content area Grid — this tells the framework to toggle child visibility
- **The content area Grid with `Region.Navigator="Visibility"` is empty or holds named children.** Keep it empty (the framework resolves registered routes and injects views at runtime), or give every pre-placed child a `uen:Region.Name` matching its route. Never add unnamed `Collapsed` pages.
- `uen:Region.Attached="True"` MUST also be on the TabBar itself
- Route names in `RouteMap` MUST match the `uen:Region.Name` values in `SHELL_PAGE_NAME.xaml`
- Tab page routes are nested under the `"Main"` route so only the content area updates, not the entire page
- Do NOT use `Region.Attached="True"` inside `Shell.xaml` — only in `SHELL_PAGE_NAME.xaml`

## Related Skills

- `references/regions.md` — Region concepts
- `references/responsive-shell.md` — Adaptive navigation
- `references/panel-visibility.md` — Visibility-based content switching details
- the `uno-toolkit` skill (`references/tabbar.md`) — TabBar control reference
- `references/navigationview.md` — Sidebar navigation alternative
- `references/routes.md` — Route registration details
