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
uno_platform_docs_fetch(sourcePath="external/uno.toolkit.ui/doc/controls-styles.md")
```

Search for ExtendedSplashScreen in the controls list.

### Step 2: For Navigation Integration

ExtendedSplashScreen often works with Navigation Extensions for Shell startup:

See the `uno-navigation` skill (`references/setup.md`).

## Key Principles (Stable)

- Based on `LoadingView` — shows native splash image with custom loading content
- Requires `Init()` call on Android in `MainActivity.OnCreate`
- Do NOT use `Region.Attached="True"` inside ExtendedSplashScreen content
- Content is shown on top of the native splash screen image
- Loading screen visuals (splash image, background color) are primarily controlled by **Resizetizer** configuration

## Related Skills

- `references/loadingview.md` — Base loading control
- the `uno-navigation` skill (`references/setup.md`) — Navigation host setup
