# Uno Toolkit Segmented Controls

## Workflow

> **Docs lookup:** call `uno_platform_docs_search(...)` first, then `uno_platform_docs_fetch(sourcePath="…")` using the `sourcePath` field from a result (a relative `.md` path; add the result's `anchor` for a section). Never pass a URL, a `.html` link, or a hand-built path.

### Step 1: Fetch the Segmented Control Documentation

```
uno_platform_docs_search("Uno Toolkit segmented control TabBar button group exclusive")
```

Primary documentation page:
- **Segmented Controls**: `external/uno.toolkit.ui/doc/controls/SegmentedControls.md`

Fetch it:

```
uno_platform_docs_fetch(sourcePath="external/uno.toolkit.ui/doc/controls/SegmentedControls.md")
```

Do not take the segmented snippet from the TabBar page; it uses `SegmentedStyle` / `SlidingSegmentedStyle`, and those keys exist nowhere.

## Critical Rules

- The segmented style keys are **`CupertinoSegmentedStyle`** and **`CupertinoSlidingSegmentedStyle`** (item styles `CupertinoSegmentedItemStyle` / `CupertinoSlidingSegmentedItemStyle`), applied on the `TabBar`. They ship only in `Uno.Toolkit.WinUI.Cupertino`; Material and Simple ship no segmented style. There is no `SegmentedStyle` or `SegmentedTabBarStyle` key; either name silently falls through to the default `TabBar` look.

## Key Principles (Stable)

- Segmented controls use `TabBar` with `Style="{StaticResource CupertinoSegmentedStyle}"` (or `CupertinoSlidingSegmentedStyle`)
- Each option is a `TabBarItem`
- Single selection mode — mutually exclusive options
- The concrete segmented-button visual comes from the active design-theme

## Related Skills

- `references/tabbar.md` — Full TabBar reference
- `references/chip.md` — Alternative selection via ChipGroup
