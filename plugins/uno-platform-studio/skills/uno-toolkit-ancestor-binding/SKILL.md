---
name: uno-toolkit-ancestor-binding
description: "Use AncestorBinding and ItemsControlBinding markup extensions for relative binding from within DataTemplates. Access parent DataContext."
when_to_use: "Use when binding inside a `DataTemplate` needs to reach the outer `DataContext`, the page's ViewModel, or an ancestor `ItemsControl` — the Uno equivalent of WPF's `RelativeSource FindAncestor`. `ItemsControlBinding` exposes the parent ItemsControl's own `DataContext` from within its item template."
metadata:
  author: uno-platform
  version: "2.4"
  category: toolkit
---

# Uno Toolkit AncestorBinding — Agent Skill

## Workflow

> **Docs lookup:** call `uno_platform_docs_search(...)` first, then `uno_platform_docs_fetch(sourcePath="…")` using the `sourcePath` field from a result (a relative `.md` path; add the result's `anchor` for a section). Never pass a URL, a `.html` link, or a hand-built path.

### Step 1: Fetch the AncestorBinding Documentation

```
uno_platform_docs_search("Uno Toolkit AncestorBinding ItemsControlBinding relative binding DataTemplate ancestor")
```

Primary documentation page:
- **AncestorBinding & ItemsControlBinding**: `external/uno.toolkit.ui/doc/helpers/ancestor-itemscontrol-binding.md`

Fetch the page:

```
uno_platform_docs_fetch(sourcePath="external/uno.toolkit.ui/doc/helpers/ancestor-itemscontrol-binding.md")
```

## Key Principles (Stable)

- `{utu:AncestorBinding AncestorType=MyControl, Path=DataContext.MyProperty}` — binds to an ancestor's DataContext
- `{utu:ItemsControlBinding Path=DataContext.MyCommand}` — shorthand for binding to the parent ItemsControl's DataContext
- Similar to WPF's `{RelativeSource Mode=FindAncestor, AncestorType=...}`
- Essential for commands in DataTemplates that need to call the page's ViewModel
- XAML namespace: `xmlns:utu="using:Uno.Toolkit.UI"`

## Related Skills

- [[uno-toolkit-command-extensions]] — Commands in templates
