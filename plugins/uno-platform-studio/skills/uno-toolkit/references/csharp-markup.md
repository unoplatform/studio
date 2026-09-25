# Uno Toolkit C# Markup

## Workflow

> **Docs lookup:** call `uno_platform_docs_search(...)` first, then `uno_platform_docs_fetch(sourcePath="…")` using the `sourcePath` field from a result (a relative `.md` path; add the result's `anchor` for a section). Never pass a URL, a `.html` link, or a hand-built path.

### Step 1: Fetch the C# Markup Toolkit Documentation

```
uno_platform_docs_search("Uno Toolkit C# Markup fluent API setup NuGet package")
```

Primary documentation pages:
- **Toolkit C# Markup**: `external/uno.toolkit.ui/doc/getting-started.md`
- **Material Toolkit C# Markup**: `external/uno.toolkit.ui/doc/material-getting-started.md`
- **Chefs C# Markup Toolkit Tutorial**: `external/uno.extensions/doc/Learn/Markup/HowTo-CustomMarkupProject-Toolkit.md`

Fetch the Chefs tutorial:

```
uno_platform_docs_fetch(sourcePath="external/uno.extensions/doc/Learn/Markup/HowTo-CustomMarkupProject-Toolkit.md")
```

## Key Principles (Stable)

- Add `Uno.Toolkit.WinUI.Markup` NuGet package
- For Material: add `Uno.Toolkit.WinUI.Material.Markup` NuGet package
- Fluent API: `new Card().Style(Theme.Card.Styles.Elevated)`
- Same controls and helpers, expressed in C# instead of XAML

## Related Skills

- `references/getting-started.md` — Base Toolkit setup
- the `uno-themes` skill (`references/material.md`) — Material C# Markup
