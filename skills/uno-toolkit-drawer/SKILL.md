---
name: uno-toolkit-drawer
description: "Use DrawerControl for swipe-gesture navigation drawers and DrawerFlyoutPresenter for bottom/side sheet flyouts."
when_to_use: "Use when adding swipe-to-open side drawers, bottom sheets, or modal flyout panels. `DrawerControl` for a full drawer layout with main + slide-in content; `DrawerFlyoutPresenter` for gesture-enabled `Flyout` presenters."
metadata:
  author: uno-platform
  version: "2.4"
  category: toolkit
---

# Uno Toolkit Drawer — Agent Skill

## Workflow

> **Docs lookup:** call `uno_platform_docs_search(...)` first, then `uno_platform_docs_fetch(sourcePath="…")` using the `sourcePath` field from a result (a relative `.md` path; add the result's `anchor` for a section). Never pass a URL, a `.html` link, or a hand-built path.

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
uno_platform_docs_search("Uno Toolkit DrawerFlyoutPresenter flyout bottom sheet gesture")
```

## Key Principles (Stable)

- `DrawerControl` has a main content area and a drawer that slides in
- `DrawerOpenDirection` — Left, Right, Up, Down
- `IsOpen` property controls drawer state (bindable)
- `DrawerFlyoutPresenter` — adds swipe gesture support to standard Flyouts
- `DrawerLength` — size of the drawer
- Supports light dismiss (tap outside to close)

## Related Skills

- [[uno-navigation-dialogs]] — Dialog/flyout via navigation
