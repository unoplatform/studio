# Uno Toolkit TabBarItem Extensions

## Key Principles (Stable)

- Attached properties for `TabBarItem` controls
- `OnClickBehaviors` (`None`, `Auto`, `BackNavigation`, `ScrollToTop`) with `OnClickBehaviorsTarget` define what re-clicking the already selected tab does; routing tabs to pages is `uen:Region` work (see the `uno-navigation` skill, `references/tabbar.md`)
- XAML namespace: `xmlns:utu="using:Uno.Toolkit.UI"`

## Docs, for what this page does not cover

Use these when the sections above do not answer the question: search, then fetch only the section you need, with the `anchor` from the search result. A whole page stays in context for the rest of the session.

### Fetch the TabBarItem Extensions Documentation

```
uno_platform_docs_search("Uno Toolkit TabBarItem extensions command badge selection indicator")
```

Primary documentation page:
- **TabBarItem Extensions**: `external/uno.toolkit.ui/doc/helpers/TabBarItem-extensions.md`

Fetch the page:

```
uno_platform_docs_fetch(sourcePath="external/uno.toolkit.ui/doc/helpers/TabBarItem-extensions.md", anchor="…")
```

## Related Skills

- `references/tabbar.md` — TabBar control reference
- `references/command-extensions.md` — General command extensions
