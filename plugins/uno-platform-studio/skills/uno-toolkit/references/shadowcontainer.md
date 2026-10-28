# Uno Toolkit ShadowContainer

## Key Principles (Stable)

- Shadow properties follow CSS `box-shadow` (`IsInner` is `inset`), with `Opacity` as a separate multiplier on `Color`
- `Shadows` property (a `ShadowCollection`) holds the `Shadow` items; `ShadowContainer` and `Shadow` ship in the `Uno.Toolkit.Skia.WinUI` package, which `<UnoFeatures>Toolkit</UnoFeatures>` alone does not reference on Uno.Sdk 6.8: use `<UnoFeatures>Toolkit;Skia</UnoFeatures>` or an explicit `Uno.Toolkit.Skia.WinUI` PackageReference (the xmlns stays `using:Uno.Toolkit.UI`)
- Each `Shadow` has: `OffsetX`, `OffsetY`, `BlurRadius`, `Spread`, `Color`, `Opacity`
- `IsInner="True"` creates an inset shadow
- **Background must be set on ShadowContainer, not on the child** for inner shadows to work
- Multiple shadows layer on top of each other for complex effects
- XAML namespace: `xmlns:utu="using:Uno.Toolkit.UI"`

## Docs, for what this page does not cover

Use these when the sections above do not answer the question: search, then fetch only the section you need, with the `anchor` from the search result. A whole page stays in context for the rest of the session.

### Fetch the ShadowContainer Documentation

```
uno_platform_docs_search("Uno Toolkit ShadowContainer shadow drop inner inset layer")
```

Primary documentation page:
- **ShadowContainer**: `external/uno.toolkit.ui/doc/controls/ShadowContainer.md`

Fetch the page:

```
uno_platform_docs_fetch(sourcePath="external/uno.toolkit.ui/doc/controls/ShadowContainer.md", anchor="…")
```

### For How-To / Walkthrough

```
uno_platform_docs_search("ShadowContainer howto walkthrough shadow example")
```

## Related Skills

- `references/card.md` — Cards with built-in elevation
- the `uno-themes` skill (`references/material.md`) — Material elevation extension
