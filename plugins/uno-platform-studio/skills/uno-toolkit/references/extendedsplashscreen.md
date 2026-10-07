# Uno Toolkit ExtendedSplashScreen

## Workflow

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
- Requires `Uno.Toolkit.UI.ExtendedSplashScreen.Init(this);` on Android in `MainActivity.OnCreate` (the signature is `Init(Activity activity)`)
- For navigation regions and splash content, see the `uno-navigation` skill (`references/setup.md`)
- Does not require a native splash screen; `Platforms` (flags: `Android,iOS,Windows,WebAssembly,Skia`, default `All`) picks where it shows
- Loading screen visuals (splash image, background color) are primarily controlled by **Resizetizer** configuration: the `UnoSplashScreen` build action (`BaseSize`, `Color`); see `uno_platform_docs_fetch(sourcePath="external/uno.resizetizer/doc/using-uno-resizetizer.md")`, section `UnoSplashScreen`

## Related Skills

- `references/loadingview.md` — Base loading control
- the `uno-navigation` skill (`references/setup.md`) — Navigation host setup
