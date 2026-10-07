# Uno Toolkit Lightweight Styling

## Workflow

### Step 1: Fetch the Lightweight Styling Documentation

```
uno_platform_docs_search("Uno Toolkit lightweight styling resource keys override controls")
```

Primary documentation page:
- **Toolkit Lightweight Styling**: `external/uno.toolkit.ui/doc/lightweight-styling.md`

Fetch the page:

```
uno_platform_docs_fetch(sourcePath="external/uno.toolkit.ui/doc/lightweight-styling.md")
```

### Step 2: For Specific Control Resource Keys

Each control's documentation page lists its resource keys. Search for the specific control:

```
uno_platform_docs_search("Uno Toolkit [ControlName] lightweight styling resource keys")
```

## Key Principles (Stable)

- Override resource keys at App, Page, or Control level
- Resource keys follow roughly `{StyleVariant}{ShortControlName}{Property}{State}` (e.g. `ChipForegroundPointerOver`, `FilledCardContentBorderBrushPressed`, `DividerSubHeaderForeground`). The control name is often shortened, so copy the exact key from the control's doc table instead of composing it
- Control-level overrides use `<Control.Resources>` or `ResourceExtensions`
- No need to redefine entire styles/templates
- Each control's doc page lists available resource keys

## Related Skills

- `references/resource-extensions.md` — Per-control resource dictionary
- the `uno-themes` skill (`references/material.md`) — Material design lightweight styling
