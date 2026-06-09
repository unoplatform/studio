---
name: uno-toolkit-chip
description: "Use Chip and ChipGroup for selection, filtering, or action triggers. Supports Assist, Input, Filter, and Suggestion chip styles."
when_to_use: "Use when building filter bars, tag input, multi-select pills, or suggestion chips — the Assist, Input, Filter, and Suggestion chip patterns. `ChipGroup` handles single or multi-selection; chips support leading icons and a remove button."
metadata:
  author: uno-platform
  version: "2.5"
  category: toolkit
---

# Uno Toolkit Chip — Agent Skill

## Workflow

> **Docs lookup:** call `uno_platform_docs_search(...)` first, then `uno_platform_docs_fetch(sourcePath="…")` using the `sourcePath` field from a result (a relative `.md` path; add the result's `anchor` for a section). Never pass a URL, a `.html` link, or a hand-built path.

### Step 1: Fetch the Chip Documentation

```
uno_platform_docs_search("Uno Toolkit Chip ChipGroup filter assist input suggestion")
```

Primary documentation page:
- **Chip & ChipGroup**: `external/uno.toolkit.ui/doc/controls/ChipAndChipGroup.md`

Fetch the page:

```
uno_platform_docs_fetch(sourcePath="external/uno.toolkit.ui/doc/controls/ChipAndChipGroup.md")
```

### Step 2: For How-To / Walkthrough

```
uno_platform_docs_search("Chip ChipGroup howto walkthrough selection filter")
```

## Key Principles (Stable)

- `Chip` is derived from `ToggleButton` — can be checked/unchecked
- Styles: `AssistChipStyle`, `InputChipStyle`, `FilterChipStyle`, `SuggestionChipStyle`
- `ChipGroup` manages a collection of chips with `SelectionMode` (Single, Multiple, None)
- `CanRemove="True"` enables a remove button on the chip
- Icon support via `Icon` property
- XAML namespace: `xmlns:utu="using:Uno.Toolkit.UI"`

## Related Skills

- [[uno-toolkit-selector-extensions]] — Selection behavior extensions
