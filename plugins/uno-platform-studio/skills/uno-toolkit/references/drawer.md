# Uno Toolkit Drawer

## Key Principles (Stable)

- `DrawerControl` has a main content area and a drawer that slides in
- `OpenDirection` (enum `DrawerOpenDirection`: Left, Right, Up, Down; default `Right`)
- `IsOpen` property controls drawer state (bindable)
- `DrawerFlyoutPresenter` — adds swipe gesture support to standard Flyouts
- `DrawerDepth` — size of the drawer (`DrawerFlyoutPresenter` uses the attached `DrawerLength` instead, a `GridLength` defaulting to `0.66*`)
- Supports light dismiss (tap outside to close)

## Docs, for what this page does not cover

Use these when the sections above do not answer the question: search, then fetch only the section you need, with the `anchor` from the search result. A whole page stays in context for the rest of the session.

### Fetch the Drawer Documentation

```
uno_platform_docs_search("Uno Toolkit DrawerControl DrawerFlyoutPresenter swipe sheet")
```

Primary documentation page:
- **DrawerControl**: `external/uno.toolkit.ui/doc/controls/DrawerControl.md`

Fetch the page:

```
uno_platform_docs_fetch(sourcePath="external/uno.toolkit.ui/doc/controls/DrawerControl.md", anchor="…")
```

### For DrawerFlyoutPresenter

If the user needs gesture-enabled flyouts:

```
uno_platform_docs_fetch(sourcePath="external/uno.toolkit.ui/doc/controls/DrawerFlyoutPresenter.md", anchor="…")
```

## Related Skills

- the `uno-navigation` skill (`references/dialogs.md`) — Dialog/flyout via navigation
