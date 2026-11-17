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

## Matching a design's header

When the design shows a header that does not look like the default bar, restyle the bar; do not replace it with a `Grid`, a `Button` and a `TextBlock`. A hand-built header has to redo what the bar does itself: show the back button only when the page's Frame can go back, and go back when it is pressed.

| The design wants | Set |
|---|---|
| A larger or differently weighted title | `Content` as an element: a `TextBlock` with one of the theme's type-scale styles. Or the `NavigationBarFontSize` resource for size alone |
| Its own back icon | `MainCommand` holding an `AppBarButton` with the `Icon` and no `Command`; `MainCommandMode` stays `Back` |
| Its own colours | The `NavigationBarBackground`, `NavigationBarForeground` and `NavigationBarMainCommandForeground` resources |
| Its own inset | The `NavigationBarPadding` resource |

```xml
<utu:NavigationBar AutomationProperties.AutomationId="DetailsNavigationBar">
  <utu:NavigationBar.Content>
    <TextBlock Text="Order details"
               Style="{StaticResource HeadlineSmall}"
               AutomationProperties.HeadingLevel="Level1" />
  </utu:NavigationBar.Content>
  <utu:NavigationBar.MainCommand>
    <AppBarButton AutomationProperties.Name="Back">
      <AppBarButton.Icon>
        <PathIcon Data="M7.4,1.4 L6,0 L0,6 L6,12 L7.4,10.6 L2.8,6 Z" />
      </AppBarButton.Icon>
    </AppBarButton>
  </utu:NavigationBar.MainCommand>
</utu:NavigationBar>
```

Override the resources once, in the app's resources, the way `references/lightweight-styling.md` describes; the NavigationBar docs page lists every key. Check the result in the running app at each width the design covers.

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
