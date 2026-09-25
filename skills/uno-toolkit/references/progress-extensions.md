# Uno Toolkit Progress Extensions

## Workflow

### Step 1: Fetch the ProgressExtensions Documentation

```
uno_platform_docs_search("Uno Toolkit ProgressExtensions progress bar ring ILoadable async command")
```

Primary documentation:

```
uno_platform_docs_search("ProgressExtensions IsActive source loadable progress")
```

## Key Principles (Stable)

- `utu:ProgressExtensions.IsExecuting` — attached boolean property on a parent element that toggles all `ProgressRing` and `ProgressBar` controls in its sub-visual-tree
- Acts on `ProgressRing.IsActive` and `ProgressBar.IsIndeterminate` properties of descendant controls
- XAML namespace: `xmlns:utu="using:Uno.Toolkit.UI"`

## Related Skills

- `references/loadingview.md` — LoadingView for full-content loading states
