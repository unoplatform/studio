# Uno Toolkit ExtendedSplashScreen

## Key Principles (Stable)

- Derives from `LoadingView` — replicates the platform's native splash look and shows your `LoadingContent` over it while `Source` is busy
- Requires `Uno.Toolkit.UI.ExtendedSplashScreen.Init(this);` on Android in `MainActivity.OnCreate` (the signature is `Init(Activity activity)`)
- For navigation regions and splash content, see the `uno-navigation` skill (`references/setup.md`)
- Does not require a native splash screen; `Platforms` (flags: `Android,iOS,Windows,WebAssembly,Skia`, default `All`) picks where it shows
- Loading screen visuals (splash image, background color) are primarily controlled by **Resizetizer** configuration: the `UnoSplashScreen` build action (`BaseSize`, `BackgroundColor`; Uno.Resizetizer 2.0 ignores the old `Color` metadata with warning `Uno.Resizetizer0001`); see `uno_platform_docs_fetch(sourcePath="external/uno.resizetizer/doc/using-uno-resizetizer.md")`, section `UnoSplashScreen`

## Docs, for what this page does not cover

Use these when the sections above do not answer the question: search, then fetch only the section you need, with the `anchor` from the search result. A whole page stays in context for the rest of the session.

### Fetch the ExtendedSplashScreen Documentation

```
uno_platform_docs_search("Uno Toolkit ExtendedSplashScreen splash loading startup Init")
```

Primary documentation page:
- **ExtendedSplashScreen**: search results will provide the control page

Fetch results and look for the control documentation:

```
uno_platform_docs_fetch(sourcePath="external/uno.toolkit.ui/doc/controls/ExtendedSplashScreen.md", anchor="…")
```

Search for ExtendedSplashScreen in the controls list.

### For Navigation Integration

ExtendedSplashScreen often works with Navigation Extensions for Shell startup:

See the `uno-navigation` skill (`references/setup.md`).

## Related Skills

- `references/loadingview.md` — Base loading control
- the `uno-navigation` skill (`references/setup.md`) — Navigation host setup
