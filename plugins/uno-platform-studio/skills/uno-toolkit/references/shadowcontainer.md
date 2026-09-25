# Uno Toolkit ShadowContainer

## Workflow

> **Docs lookup:** call `uno_platform_docs_search(...)` first, then `uno_platform_docs_fetch(sourcePath="…")` using the `sourcePath` field from a result (a relative `.md` path; add the result's `anchor` for a section). Never pass a URL, a `.html` link, or a hand-built path.

### Step 1: Fetch the ShadowContainer Documentation

```
uno_platform_docs_search("Uno Toolkit ShadowContainer shadow drop inner inset layer")
```

Primary documentation page:
- **ShadowContainer**: `external/uno.toolkit.ui/doc/controls/ShadowContainer.md`

Fetch the page:

```
uno_platform_docs_fetch(sourcePath="external/uno.toolkit.ui/doc/controls/ShadowContainer.md")
```

### Step 2: For How-To / Walkthrough

```
uno_platform_docs_search("ShadowContainer howto walkthrough shadow example")
```

## Key Principles (Stable)

- Shadow properties work like CSS `box-shadow` — the API is a 1-to-1 mapping
- `ShadowCollection` property contains a collection of `Shadow` items
- Each `Shadow` has: `OffsetX`, `OffsetY`, `BlurRadius`, `SpreadRadius`, `Color`, `Opacity`
- `IsInner="True"` creates an inset shadow
- **Background must be set on ShadowContainer, not on the child** for inner shadows to work
- Multiple shadows layer on top of each other for complex effects
- XAML namespace: `xmlns:utu="using:Uno.Toolkit.UI"`

## Related Skills

- `references/card.md` — Cards with built-in elevation
- the `uno-themes` skill (`references/material.md`) — Material elevation extension
