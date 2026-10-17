# Uno Toolkit NavigationBar

## Critical Rules

- **`MainCommand` is an `AppBarButton`** — there is no dedicated `NavigationBarMainCommand` type. `PrimaryCommands` and `SecondaryCommands` take `ICommandBarElement`s (`AppBarButton`, `AppBarToggleButton`, `AppBarSeparator`).
- **Omit `MainCommand` for plain back navigation.** Under the default `MainCommandMode="Back"` the bar already calls `Frame.GoBack()` on click, so `<AppBarButton Command="{Binding GoBackCommand}"/>` navigates back twice. Set a custom `MainCommand` with a `Command` only together with `MainCommandMode="Action"` (burger menu, confirm-before-leave); set `MainCommand` without a `Command` only to swap the icon.

## Key Principles (Stable)

- `MainCommand` — the back-button slot; an `AppBarButton` instance. `MainCommandMode` is `Back` (default, the bar navigates back itself) or `Action` (your `Command` runs instead)
- `PrimaryCommands` — action `ICommandBarElement`s displayed on the right
- `SecondaryCommands` — overflow `ICommandBarElement`s in the overflow menu
- XAML-drawn on every platform, look set by the theme's `NavigationBarStyle` / `MaterialNavigationBarStyle` family of keys; the native iOS/Android bar is gone in Uno.Toolkit 11 (Uno Platform 7)
- `Content` is the title; set it as an attribute (`Content="Title"`). The XAML content property is `PrimaryCommands`, so child elements become primary commands
- XAML namespace: `xmlns:utu="using:Uno.Toolkit.UI"`

## Docs, for what this page does not cover

Use these when the sections above do not answer the question: search, then fetch only the section you need, with the `anchor` from the search result. A whole page stays in context for the rest of the session.

### Fetch the NavigationBar Documentation

```
uno_platform_docs_search("Uno Toolkit NavigationBar app bar MainCommand PrimaryCommands")
```

Primary documentation page:
- **NavigationBar**: `external/uno.toolkit.ui/doc/controls/NavigationBar.md`

Fetch the page:

```
uno_platform_docs_fetch(sourcePath="external/uno.toolkit.ui/doc/controls/NavigationBar.md", anchor="…")
```

### For Chefs Examples

```
uno_platform_docs_search("NavigationBar Chefs app example navigation back")
```

## Related Skills

- `references/safearea.md` — Safe area insets with NavigationBar
- the `uno-navigation` skill (`references/code.md`) — Navigation integration
