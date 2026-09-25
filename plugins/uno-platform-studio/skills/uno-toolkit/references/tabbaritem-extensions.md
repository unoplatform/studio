# Uno Toolkit TabBarItem Extensions

## Workflow

> **Docs lookup:** call `uno_platform_docs_search(...)` first, then `uno_platform_docs_fetch(sourcePath="…")` using the `sourcePath` field from a result (a relative `.md` path; add the result's `anchor` for a section). Never pass a URL, a `.html` link, or a hand-built path.

### Step 1: Fetch the TabBarItem Extensions Documentation

```
uno_platform_docs_search("Uno Toolkit TabBarItem extensions command badge selection indicator")
```

Primary documentation page:
- **TabBarItem Extensions**: `external/uno.toolkit.ui/doc/helpers/TabBarItem-extensions.md`

Fetch the page:

```
uno_platform_docs_fetch(sourcePath="external/uno.toolkit.ui/doc/helpers/TabBarItem-extensions.md")
```

## Key Principles (Stable)

- Attached properties for `TabBarItem` controls
- Enables on-click navigation behavior
- XAML namespace: `xmlns:utu="using:Uno.Toolkit.UI"`

## Related Skills

- `references/tabbar.md` — TabBar control reference
- `references/command-extensions.md` — General command extensions
