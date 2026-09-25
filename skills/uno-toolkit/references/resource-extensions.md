# Uno Toolkit Resource Extensions

## Workflow

> **Docs lookup:** call `uno_platform_docs_search(...)` first, then `uno_platform_docs_fetch(sourcePath="…")` using the `sourcePath` field from a result (a relative `.md` path; add the result's `anchor` for a section). Never pass a URL, a `.html` link, or a hand-built path.

### Step 1: Fetch the ResourceExtensions Documentation

```
uno_platform_docs_search("Uno Toolkit ResourceExtensions ResourceDictionary style lightweight inline")
```

Primary documentation pages:
- **ResourceExtensions (Chefs)**: `external/uno.chefs/doc/toolkit/ResourceExtensions.md`

Fetch the Chefs page:

```
uno_platform_docs_fetch(sourcePath="external/uno.chefs/doc/toolkit/ResourceExtensions.md")
```

### Step 2: For Reference Documentation

```
uno_platform_docs_search("ResourceExtensions Resources attached property dictionary override")
```

## Key Principles (Stable)

- `utu:ResourceExtensions.Resources` — attaches a ResourceDictionary to a control
- Enables per-control resource key overrides without nesting in page resources
- Great for creating multiple visual variants of the same control
- XAML namespace: `xmlns:utu="using:Uno.Toolkit.UI"`

## Related Skills

- `references/lightweight-styling.md` — General lightweight styling approach
- the `uno-themes` skill (`references/material.md`) — Material lightweight styling
