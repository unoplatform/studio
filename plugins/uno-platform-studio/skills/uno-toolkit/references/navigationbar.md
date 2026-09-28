# Uno Toolkit NavigationBar

## Workflow

> **Docs lookup:** call `uno_platform_docs_search(...)` first, then `uno_platform_docs_fetch(sourcePath="…")` using the `sourcePath` field from a result (a relative `.md` path; add the result's `anchor` for a section). Never pass a URL, a `.html` link, or a hand-built path.

### Step 1: Fetch the NavigationBar Documentation

```
uno_platform_docs_search("Uno Toolkit NavigationBar app bar MainCommand PrimaryCommands")
```

Primary documentation page:
- **NavigationBar**: `external/uno.toolkit.ui/doc/controls/NavigationBar.md`

Fetch the page:

```
uno_platform_docs_fetch(sourcePath="external/uno.toolkit.ui/doc/controls/NavigationBar.md")
```

### Step 2: For Chefs Examples

```
uno_platform_docs_search("NavigationBar Chefs app example navigation back")
```

## Critical Rules

- **`MainCommand` is an `AppBarButton`** — there is no dedicated `NavigationBarMainCommand` type. `PrimaryCommands` and `SecondaryCommands` take `ICommandBarElement`s (`AppBarButton`, `AppBarToggleButton`, `AppBarSeparator`).
- **Omit `MainCommand` for plain back navigation.** Under the default `MainCommandMode="Back"` the bar already calls `Frame.GoBack()` on click, so `<AppBarButton Command="{Binding GoBackCommand}"/>` navigates back twice. Set a custom `MainCommand` with a `Command` only together with `MainCommandMode="Action"` (burger menu, confirm-before-leave); set `MainCommand` without a `Command` only to swap the icon.

## Key Principles (Stable)

- `MainCommand` — the back-button slot; an `AppBarButton` instance. `MainCommandMode` is `Back` (default, the bar navigates back itself) or `Action` (your `Command` runs instead)
- `PrimaryCommands` — action `ICommandBarElement`s displayed on the right
- `SecondaryCommands` — overflow `ICommandBarElement`s in the overflow menu
- Two rendering modes: `Windows` (XAML drawn) and `Native` (platform AppBar on iOS/Android)
- Content property is the title
- XAML namespace: `xmlns:utu="using:Uno.Toolkit.UI"`

## Related Skills

- `references/safearea.md` — Safe area insets with NavigationBar
- the `uno-navigation` skill (`references/code.md`) — Navigation integration
