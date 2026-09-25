# Uno Toolkit LoadingView

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

- `references/progress-extensions.md` — ProgressBar/ProgressRing loading binding
- `references/extendedsplashscreen.md` — App startup loading
