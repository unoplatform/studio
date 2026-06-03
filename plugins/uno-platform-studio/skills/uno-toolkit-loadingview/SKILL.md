---
name: uno-toolkit-loadingview
description: "Use LoadingView to display loading indicators while async operations execute. Binds to ILoadable sources like AsyncCommand."
when_to_use: "Use when a view must show a spinner or skeleton while an `IAsyncCommand`, `IFeed`, or `IState` is in flight, binding `LoadingView` to any `ILoadable` source, composing multiple in-flight operations via `CompositeLoadableSource`, or toggling the loading visual automatically as commands execute."
metadata:
  author: uno-platform
  version: "2.4"
  category: toolkit
---

# Uno Toolkit LoadingView — Agent Skill

## Workflow

> **Docs lookup:** call `uno_platform_docs_search(...)` first, then `uno_platform_docs_fetch(sourcePath="…")` using the `sourcePath` field from a result (a relative `.md` path; add the result's `anchor` for a section). Never pass a URL, a `.html` link, or a hand-built path.

### Step 1: Fetch the LoadingView Documentation

```
uno_platform_docs_search("Uno Toolkit LoadingView loading indicator ILoadable AsyncCommand")
```

Primary documentation page:
- **LoadingView**: `external/uno.toolkit.ui/doc/controls/LoadingView.md`

Fetch the page:

```
uno_platform_docs_fetch(sourcePath="external/uno.toolkit.ui/doc/controls/LoadingView.md")
```

### Step 2: For How-To / Walkthrough

```
uno_platform_docs_search("LoadingView howto walkthrough AsyncCommand loading")
```

## Key Principles (Stable)

- `Source` property binds to an `ILoadable` (like `AsyncCommand`)
- Loading content is displayed when the source is busy
- `CompositeLoadableSource` combines multiple ILoadable sources
- `LoadingContent` — custom content shown during loading (default is ProgressRing)
- XAML namespace: `xmlns:utu="using:Uno.Toolkit.UI"`

## Related Skills

- [[uno-toolkit-progress-extensions]] — ProgressBar/ProgressRing loading binding
- [[uno-toolkit-extendedsplashscreen]] — App startup loading
