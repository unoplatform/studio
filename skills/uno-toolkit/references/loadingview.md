# Uno Toolkit LoadingView

## Key Principles (Stable)

- `Source` property binds to an `ILoadable` (`Uno.Toolkit.ILoadable`, in the `Uno.Toolkit` package): `bool IsExecuting` plus an `IsExecutingChanged` event
- The Toolkit ships no `AsyncCommand` type; the docs' `AsyncCommand` is sample code. Implement `ILoadable` on your own command, or wrap any `ILoadable` in `LoadableSource`
- Loading content is displayed when the source is busy
- `CompositeLoadableSource` combines multiple `ILoadable`s through its `Sources` collection (any `ILoadable`, not only `LoadableSource`)
- `LoadingContent` — content shown during loading. It defaults to null, so put `<ProgressRing IsActive="True" />` (or a skeleton) in it or nothing appears
- XAML namespace: `xmlns:utu="using:Uno.Toolkit.UI"`

## Docs, for what this page does not cover

Use these when the sections above do not answer the question: search, then fetch only the section you need, with the `anchor` from the search result. A whole page stays in context for the rest of the session.

### Fetch the LoadingView Documentation

```
uno_platform_docs_search("Uno Toolkit LoadingView loading indicator ILoadable")
```

Primary documentation page:
- **LoadingView**: `external/uno.toolkit.ui/doc/controls/LoadingView.md`

Fetch the page:

```
uno_platform_docs_fetch(sourcePath="external/uno.toolkit.ui/doc/controls/LoadingView.md", anchor="…")
```

### For How-To / Walkthrough

```
uno_platform_docs_search("LoadingView howto walkthrough loading")
```

## Related Skills

- `references/progress-extensions.md` — ProgressBar/ProgressRing loading binding
- `references/extendedsplashscreen.md` — App startup loading
