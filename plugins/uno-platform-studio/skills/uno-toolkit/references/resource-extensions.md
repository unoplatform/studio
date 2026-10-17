# Uno Toolkit Resource Extensions

## Key Principles (Stable)

- `utu:ResourceExtensions.Resources` — attaches a ResourceDictionary to a control
- Enables per-control resource key overrides without nesting in page resources
- Great for creating multiple visual variants of the same control
- XAML namespace: `xmlns:utu="using:Uno.Toolkit.UI"`

## Docs, for what this page does not cover

Use these when the sections above do not answer the question: search, then fetch only the section you need, with the `anchor` from the search result. A whole page stays in context for the rest of the session.

### Fetch the ResourceExtensions Documentation

```
uno_platform_docs_search("Uno Toolkit ResourceExtensions ResourceDictionary style lightweight inline")
```

Primary documentation pages:
- **ResourceExtensions (official helper page)**: `external/uno.toolkit.ui/doc/helpers/resource-extensions.md`
- **ResourceExtensions (Chefs recipe)**: `external/uno.chefs/doc/toolkit/ResourceExtensions.md`

Fetch the official page first, then the Chefs recipe:

```
uno_platform_docs_fetch(sourcePath="external/uno.toolkit.ui/doc/helpers/resource-extensions.md", anchor="…")
uno_platform_docs_fetch(sourcePath="external/uno.chefs/doc/toolkit/ResourceExtensions.md", anchor="…")
```

### For Reference Documentation

```
uno_platform_docs_search("ResourceExtensions Resources attached property dictionary override")
```

## Related Skills

- `references/lightweight-styling.md` — General lightweight styling approach
- the `uno-themes` skill (`references/material-customization.md`) — Material lightweight styling
