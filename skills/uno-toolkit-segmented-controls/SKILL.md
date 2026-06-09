---
name: uno-toolkit-segmented-controls
description: "Segmented button groups implemented as a `TabBar` with the segmented style."
when_to_use: "Use when offering 2–5 mutually exclusive inline options (filter modes, view switchers) — the segmented button pattern."
metadata:
  author: uno-platform
  version: "2.6"
  category: toolkit
---

# Uno Toolkit Segmented Controls — Agent Skill

## Workflow

> **Docs lookup:** call `uno_platform_docs_search(...)` first, then `uno_platform_docs_fetch(sourcePath="…")` using the `sourcePath` field from a result (a relative `.md` path; add the result's `anchor` for a section). Never pass a URL, a `.html` link, or a hand-built path.

### Step 1: Fetch the Segmented Control Documentation

```
uno_platform_docs_search("Uno Toolkit segmented control TabBar button group exclusive")
```

Primary documentation pages:
- **TabBar & TabBarItem** (segmented section): `external/uno.toolkit.ui/doc/controls/TabBarAndTabBarItem.md`

Fetch the TabBar page and look for segmented control styles:

```
uno_platform_docs_fetch(sourcePath="external/uno.toolkit.ui/doc/controls/TabBarAndTabBarItem.md")
```

## Critical Rules

- The segmented style key is **`SegmentedStyle`** — apply it on the `TabBar` (`Style="{StaticResource SegmentedStyle}"`). There is no `SegmentedTabBarStyle` key; using that name will silently fall through to the default `TabBar` look.

## Key Principles (Stable)

- Segmented controls use `TabBar` with `Style="{StaticResource SegmentedStyle}"`
- Each option is a `TabBarItem`
- Single selection mode — mutually exclusive options
- The concrete segmented-button visual comes from the active design-theme

## Related Skills

- [[uno-toolkit-tabbar]] — Full TabBar reference
- [[uno-toolkit-chip]] — Alternative selection via ChipGroup
