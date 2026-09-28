# Uno Toolkit ExtendedSplashScreen

## Workflow

> **Docs lookup:** call `uno_platform_docs_search(...)` first, then `uno_platform_docs_fetch(sourcePath="…")` using the `sourcePath` field from a result (a relative `.md` path; add the result's `anchor` for a section). Never pass a URL, a `.html` link, or a hand-built path.

### Step 1: Fetch the ExtendedSplashScreen Documentation

```
uno_platform_docs_search("Uno Toolkit ExtendedSplashScreen splash loading startup Init")
```

Primary documentation page:
- **ExtendedSplashScreen**: search results will provide the control page

Fetch results and look for the control documentation:

```
uno_platform_docs_fetch(sourcePath="external/uno.toolkit.ui/doc/controls/ExtendedSplashScreen.md")
```

Search for ExtendedSplashScreen in the controls list.

### Step 2: For Navigation Integration

ExtendedSplashScreen often works with Navigation Extensions for Shell startup:

See the `uno-navigation` skill (`references/setup.md`).

## Key Principles (Stable)

- Derives from `LoadingView` — replicates the platform's native splash look and shows your `LoadingContent` over it while `Source` is busy
- Requires `Init()` call on Android in `MainActivity.OnCreate`
- For navigation regions and splash content, see the `uno-navigation` skill (`references/setup.md`)
- Does not require a native splash screen; `Platforms` (flags: `Android,iOS,Windows,WebAssembly,Skia`, default `All`) picks where it shows
- Loading screen visuals (splash image, background color) are primarily controlled by **Resizetizer** configuration

## Related Skills

- `references/loadingview.md` — Base loading control
- the `uno-navigation` skill (`references/setup.md`) — Navigation host setup
