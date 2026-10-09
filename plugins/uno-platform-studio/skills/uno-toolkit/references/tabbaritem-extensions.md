# Uno Toolkit TabBarItem Extensions

## Workflow

### Step 1: Fetch the TabBarItem Extensions Documentation

```
uno_platform_docs_search("Uno Toolkit TabBarItem extensions command badge selection indicator")
```

Primary documentation page:
- **TabBarItem Extensions**: `external/uno.toolkit.ui/doc/helpers/TabBarItem-extensions.md`

Fetch the page:

```
uno_platform_docs_fetch(sourcePath="external/uno.toolkit.ui/doc/helpers/TabBarItem-extensions.md")
```

## Key Principles (Stable)

- Attached properties for `TabBarItem` controls
- `OnClickBehaviors` (`None`, `Auto`, `BackNavigation`, `ScrollToTop`) with `OnClickBehaviorsTarget` define what re-clicking the already selected tab does; routing tabs to pages is `uen:Region` work (see the `uno-navigation` skill, `references/tabbar.md`)
- XAML namespace: `xmlns:utu="using:Uno.Toolkit.UI"`

## Related Skills

- `references/tabbar.md` — TabBar control reference
- `references/command-extensions.md` — General command extensions
