# Uno Toolkit VisualStateManager Extensions

## Workflow

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
- **Important**: `States` goes on a `Control` (a `Page`, `UserControl`, or templated control). `VisualStateManager.VisualStateGroups` goes on that control's **first child** (the root `Grid`), never on the same element; on the same element the transition silently fails
- XAML namespace: `xmlns:utu="using:Uno.Toolkit.UI"`

## Related Skills

- `references/responsive.md` — Responsive layout based on screen size
