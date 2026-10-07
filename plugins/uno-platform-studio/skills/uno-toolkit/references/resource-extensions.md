# Uno Toolkit Resource Extensions

## Workflow

### Step 1: Fetch the ResourceExtensions Documentation

```
uno_platform_docs_search("Uno Toolkit ResourceExtensions ResourceDictionary style lightweight inline")
```

Primary documentation pages:
- **ResourceExtensions (official helper page)**: `external/uno.toolkit.ui/doc/helpers/resource-extensions.md`
- **ResourceExtensions (Chefs recipe)**: `external/uno.chefs/doc/toolkit/ResourceExtensions.md`

Fetch the official page first, then the Chefs recipe:

```
uno_platform_docs_fetch(sourcePath="external/uno.toolkit.ui/doc/helpers/resource-extensions.md")
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
- the `uno-themes` skill (`references/material-customization.md`) — Material lightweight styling
