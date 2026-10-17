# Uno Toolkit Lightweight Styling

## Key Principles (Stable)

- Override resource keys at App, Page, or Control level
- Resource keys follow roughly `{StyleVariant}{ShortControlName}{Property}{State}` (e.g. `ChipForegroundPointerOver` and `FilledCardContentBorderBrushPressed` under Material; `ChipForegroundChecked` and `DividerSubHeaderForeground` under Simple, which ships fewer state keys). The control name is often shortened, so copy the exact key from the control's doc table instead of composing it
- Control-level overrides use `<Control.Resources>` or `ResourceExtensions`
- No need to redefine entire styles/templates
- Each control's doc page lists available resource keys

## Docs, for what this page does not cover

Use these when the sections above do not answer the question: search, then fetch only the section you need, with the `anchor` from the search result. A whole page stays in context for the rest of the session.

### Fetch the Lightweight Styling Documentation

```
uno_platform_docs_search("Uno Toolkit lightweight styling resource keys override controls")
```

Primary documentation page:
- **Toolkit Lightweight Styling**: `external/uno.toolkit.ui/doc/lightweight-styling.md`

Fetch the page:

```
uno_platform_docs_fetch(sourcePath="external/uno.toolkit.ui/doc/lightweight-styling.md", anchor="…")
```

### For Specific Control Resource Keys

Each control's documentation page lists its resource keys. Search for the specific control:

```
uno_platform_docs_search("Uno Toolkit [ControlName] lightweight styling resource keys")
```

## Related Skills

- `references/resource-extensions.md` — Per-control resource dictionary
- the `uno-themes` skill (`references/material-customization.md`) — Material design lightweight styling
