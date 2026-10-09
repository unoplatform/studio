# Uno Toolkit Cupertino Theme

## Workflow

### Step 1: Fetch the Cupertino Toolkit Documentation

```
uno_platform_docs_search("Uno Cupertino Toolkit CupertinoToolkitResources getting started")
```

Primary documentation page:
- **Cupertino Toolkit Getting Started**: `external/uno.toolkit.ui/doc/cupertino-getting-started.md`

Fetch the page:

```
uno_platform_docs_fetch(sourcePath="external/uno.toolkit.ui/doc/cupertino-getting-started.md")
```

## Key Principles (Stable)

- `CupertinoToolkitResources` (`xmlns="using:Uno.Toolkit.UI.Cupertino"`) merges only its own Toolkit styles. There is no `CupertinoToolkitTheme` type that pulls in the rest, so the full setup is:
  1. `<UnoFeatures>Toolkit;Cupertino</UnoFeatures>`
  2. In App.xaml `MergedDictionaries`: `<CupertinoColors xmlns="using:Uno.Cupertino" />`, `<CupertinoFonts xmlns="using:Uno.Cupertino" />`, `<CupertinoResources xmlns="using:Uno.Cupertino" />`
  3. Then `<ToolkitResources xmlns="using:Uno.Toolkit.UI" />` and `<CupertinoToolkitResources xmlns="using:Uno.Toolkit.UI.Cupertino" />`
- Cupertino ships only 6 Toolkit style keys: `CupertinoBottomTabBarStyle`, `CupertinoBottomTabBarItemStyle`, `CupertinoSegmentedStyle`, `CupertinoSegmentedItemStyle`, `CupertinoSlidingSegmentedStyle`, `CupertinoSlidingSegmentedItemStyle`. Card, Chip, and the unprefixed TabBar keys do not resolve under Cupertino.
- Pick one design language per app: `MaterialToolkitTheme` for Material, `SimpleToolkitTheme` for Simple, the dictionaries above for Cupertino

## Related Skills

- `references/getting-started.md` — Base Toolkit setup
- `references/material-theme.md` — Material alternative
