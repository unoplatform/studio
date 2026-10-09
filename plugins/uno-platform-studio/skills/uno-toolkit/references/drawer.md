# Uno Toolkit Drawer

## Workflow

### Step 1: Fetch the Drawer Documentation

```
uno_platform_docs_search("Uno Toolkit DrawerControl DrawerFlyoutPresenter swipe sheet")
```

Primary documentation page:
- **DrawerControl**: `external/uno.toolkit.ui/doc/controls/DrawerControl.md`

Fetch the page:

```
uno_platform_docs_fetch(sourcePath="external/uno.toolkit.ui/doc/controls/DrawerControl.md")
```

### Step 2: For DrawerFlyoutPresenter

If the user needs gesture-enabled flyouts:

```
uno_platform_docs_fetch(sourcePath="external/uno.toolkit.ui/doc/controls/DrawerFlyoutPresenter.md")
```

## Key Principles (Stable)

- `DrawerControl` has a main content area and a drawer that slides in
- `OpenDirection` (enum `DrawerOpenDirection`: Left, Right, Up, Down; default `Right`)
- `IsOpen` property controls drawer state (bindable)
- `DrawerFlyoutPresenter` — adds swipe gesture support to standard Flyouts
- `DrawerDepth` — size of the drawer (`DrawerFlyoutPresenter` uses the attached `DrawerLength` instead, a `GridLength` defaulting to `0.66*`)
- Supports light dismiss (tap outside to close)

## Related Skills

- the `uno-navigation` skill (`references/dialogs.md`) — Dialog/flyout via navigation
