# Uno Toolkit Progress Extensions

## Key Principles (Stable)

- `utu:ProgressExtensions.IsActive` — attached boolean property (there is no `IsExecuting`; that name belongs to `ILoadable`) on a parent element that toggles all `ProgressRing` and `ProgressBar` controls in its sub-visual-tree
- Acts on `ProgressRing.IsActive` and `ProgressBar.IsIndeterminate` properties of descendant controls
- XAML namespace: `xmlns:utu="using:Uno.Toolkit.UI"`

## Docs, for what this page does not cover

Use these when the sections above do not answer the question: search, then fetch only the section you need, with the `anchor` from the search result. A whole page stays in context for the rest of the session.

### Fetch the ProgressExtensions Documentation

```
uno_platform_docs_search("Uno Toolkit ProgressExtensions progress bar ring ILoadable async command")
```

Primary documentation:

```
uno_platform_docs_fetch(sourcePath="external/uno.toolkit.ui/doc/helpers/progress-extensions.md", anchor="…")
```

## Related Skills

- `references/loadingview.md` — LoadingView for full-content loading states
