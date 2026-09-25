# Uno Toolkit VisualStateManager Extensions

## Workflow

> **Docs lookup:** call `uno_platform_docs_search(...)` first, then `uno_platform_docs_fetch(sourcePath="…")` using the `sourcePath` field from a result (a relative `.md` path; add the result's `anchor` for a section). Never pass a URL, a `.html` link, or a hand-built path.

### Step 1: Fetch the VisualStateManager Extensions Documentation

```
uno_platform_docs_search("Uno Toolkit VisualStateManager extensions data driven visual state binding")
```

Primary documentation page:
- **VisualStateManager Extensions**: `external/uno.toolkit.ui/doc/helpers/VisualStateManager-extensions.md`

Fetch the page:

```
uno_platform_docs_fetch(sourcePath="external/uno.toolkit.ui/doc/helpers/VisualStateManager-extensions.md")
```

## Key Principles (Stable)

- `utu:VisualStateManagerExtensions.States` — attached property bound to a string property (note: **States** plural)
- Supports comma, semicolon, or space separated list for multiple concurrent states from different VisualStateGroups
- When the bound property changes, the matching VisualState is activated
- Property value maps directly to VisualState name
- Eliminates need for code-behind `VisualStateManager.GoToState()` calls
- **Important**: `VisualStateManager.VisualStateGroups` must be defined on the same element that has the `States` attached property
- XAML namespace: `xmlns:utu="using:Uno.Toolkit.UI"`

## Related Skills

- `references/responsive.md` — Responsive layout based on screen size
