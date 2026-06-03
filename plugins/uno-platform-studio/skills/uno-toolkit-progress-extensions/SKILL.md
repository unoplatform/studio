---
name: uno-toolkit-progress-extensions
description: "Use ProgressExtensions to toggle all ProgressRing and ProgressBar controls under a sub-visual-tree with a single binding."
when_to_use: "Use when toggling every `ProgressRing` and `ProgressBar` inside a subtree with one boolean binding, instead of binding each control individually."
metadata:
  author: uno-platform
  version: "2.4"
  category: toolkit
---

# Uno Toolkit Progress Extensions — Agent Skill

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

- [[uno-toolkit-loadingview]] — LoadingView for full-content loading states
