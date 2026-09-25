# Uno Toolkit TabBar

## Workflow

> **Docs lookup:** call `uno_platform_docs_search(...)` first, then `uno_platform_docs_fetch(sourcePath="…")` using the `sourcePath` field from a result (a relative `.md` path; add the result's `anchor` for a section). Never pass a URL, a `.html` link, or a hand-built path.

### Step 1: Fetch the TabBar Documentation

```
uno_platform_docs_search("Uno Toolkit TabBar TabBarItem bottom navigation styles icons badges")
```

Primary documentation page:
- **TabBar & TabBarItem**: `external/uno.toolkit.ui/doc/controls/TabBarAndTabBarItem.md`

Fetch the page:

```
uno_platform_docs_fetch(sourcePath="external/uno.toolkit.ui/doc/controls/TabBarAndTabBarItem.md")
```

### Step 2: For TabBar with Navigation Extensions

If using TabBar as a navigation control:

See the `uno-navigation` skill (`references/tabbar.md`) for region-based navigation integration.

### Step 3: For Chefs TabBar Example

```
uno_platform_docs_search("Chefs TabBar navigate bottom tab example")
```

Key page:
- **Chefs Navigate TabBar**: `external/uno.chefs/doc/toolkit/NavigateTabBar.md`

## Key Principles (Stable)

- **Always apply a style** to TabBar for proper appearance (e.g., `BottomTabBarStyle`, `TopTabBarStyle`, `VerticalTabBarStyle`)
- Styling goes on the **TabBar container**, not individual TabBarItems
- `TabBarItem` supports `Icon`, `Content` (label), and `Badge`
- For navigation, use with `uen:Region.Name` on each TabBarItem
- XAML namespace: `xmlns:utu="using:Uno.Toolkit.UI"`

## Related Skills

- the `uno-navigation` skill (`references/tabbar.md`) — TabBar with Navigation Extensions
- `references/tabbaritem-extensions.md` — TabBarItem attached properties
- `references/segmented-controls.md` — Segmented control style
