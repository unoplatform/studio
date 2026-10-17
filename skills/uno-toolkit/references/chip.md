# Uno Toolkit Chip

## Key Principles (Stable)

- `Chip` is derived from `ToggleButton` — can be checked/unchecked
- Styles: `AssistChipStyle`, `InputChipStyle`, `FilterChipStyle`, `SuggestionChipStyle` (Material and Simple only; Cupertino ships no Chip styles)
- `ChipGroup` manages a collection of chips with `SelectionMode` (None, Single, SingleOrNone, Multiple)
- `ChipGroup` selection surface: `SelectedItem`, `SelectedItems`, `SelectionMemberPath`, and the `ItemClick`, `ItemChecked`, `ItemUnchecked`, `ItemRemoving`, `ItemRemoved` events
- `CanRemove="True"` enables a remove button on the chip
- Icon support via `Icon` property
- XAML namespace: `xmlns:utu="using:Uno.Toolkit.UI"`

## Docs, for what this page does not cover

Use these when the sections above do not answer the question: search, then fetch only the section you need, with the `anchor` from the search result. A whole page stays in context for the rest of the session.

### Fetch the Chip Documentation

```
uno_platform_docs_search("Uno Toolkit Chip ChipGroup filter assist input suggestion")
```

Primary documentation page:
- **Chip & ChipGroup**: `external/uno.toolkit.ui/doc/controls/ChipAndChipGroup.md`

Fetch the page:

```
uno_platform_docs_fetch(sourcePath="external/uno.toolkit.ui/doc/controls/ChipAndChipGroup.md", anchor="…")
```

### For How-To / Walkthrough

```
uno_platform_docs_search("Chip ChipGroup howto walkthrough selection filter")
```

## Related Skills

- `references/segmented-controls.md` — Alternative for mutually exclusive options
