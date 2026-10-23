# Uno Toolkit TabBar

## Key Principles (Stable)

- **Always apply a style** to TabBar for proper appearance (e.g., `BottomTabBarStyle`, `TopTabBarStyle`, `VerticalTabBarStyle`)
- Styling goes on the **TabBar container**, not individual TabBarItems
- `TabBarItem` supports `Icon`, `Content` (label), `BadgeValue`, and `BadgeVisibility`. Badges render only under the Material `BottomTabBarItemStyle` / `VerticalTabBarItemStyle` templates; Simple and Cupertino ignore them
- For navigation, use with `uen:Region.Name` on each TabBarItem
- XAML namespace: `xmlns:utu="using:Uno.Toolkit.UI"`

## Docs, for what this page does not cover

Use these when the sections above do not answer the question: search, then fetch only the section you need, with the `anchor` from the search result. A whole page stays in context for the rest of the session.

### Fetch the TabBar Documentation

```
uno_platform_docs_search("Uno Toolkit TabBar TabBarItem bottom navigation styles icons badges")
```

Primary documentation page:
- **TabBar & TabBarItem**: `external/uno.toolkit.ui/doc/controls/TabBarAndTabBarItem.md`

Fetch the page:

```
uno_platform_docs_fetch(sourcePath="external/uno.toolkit.ui/doc/controls/TabBarAndTabBarItem.md", anchor="…")
```

The page is about 6,000 tokens. Its sections: `tabbaritem-style` (the item styles and which goes on the container), `vertical-tabbar-style` (a side rail with `VerticalTabBarStyle`). For a vertical navigation rail walkthrough, `external/uno.toolkit.ui/doc/controls/walkthroughs/TabBar-and-TabBarItem.howto.md`, anchor `make-a-vertical-navigation-rail`.

### For TabBar with Navigation Extensions

If using TabBar as a navigation control:

See the `uno-navigation` skill (`references/tabbar.md`) for region-based navigation integration.

### For Chefs TabBar Example

```
uno_platform_docs_search("Chefs TabBar navigate bottom tab example")
```

Key page:
- **Chefs Navigate TabBar**: `external/uno.chefs/doc/toolkit/NavigateTabBar.md`

## Related Skills

- the `uno-navigation` skill (`references/tabbar.md`) — TabBar with Navigation Extensions
- `references/tabbaritem-extensions.md` — TabBarItem attached properties
- `references/segmented-controls.md` — Segmented control style
