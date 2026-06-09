---
name: uno-navigation-contentcontrol
description: "Implement ContentControl region navigation for dynamic content areas without back stack."
when_to_use: "Use when swapping content in a specific area of the layout without maintaining back stack, using ContentControl as a navigation target region, or simple content switching that doesn't need navigation history."
metadata:
  author: uno-platform
  version: "2.3"
  category: navigation
---

# Uno Navigation ContentControl — Agent Skill

## Workflow

> **Docs lookup:** call `uno_platform_docs_search(...)` first, then `uno_platform_docs_fetch(sourcePath="…")` using the `sourcePath` field from a result (a relative `.md` path; add the result's `anchor` for a section). Never pass a URL, a `.html` link, or a hand-built path.

### Step 1: Fetch the ContentControl Navigation Documentation

```
uno_platform_docs_search("Uno Navigation ContentControl region content switching")
```

Primary documentation pages:
- **Define Regions**: `external/uno.extensions/doc/Learn/Navigation/Walkthrough/DefineRegions.md`
- **Navigation Region Reference**: `external/uno.extensions/doc/Reference/Navigation/NavigationRegion.md`

Fetch the regions reference:

```
uno_platform_docs_fetch(sourcePath="external/uno.extensions/doc/Reference/Navigation/NavigationRegion.md")
```

### Step 2: For Alternative Approaches

If the user needs visibility-based switching instead:

See the `uno-navigation-panel-visibility` skill.

## Key Principles (Stable)

- ContentControl can be a navigation target region
- No back stack — content simply replaced
- Use `uen:Region.Attached="True"` on the ContentControl
- Good for sidebar detail panes, settings containers, or single-area content swaps

## Related Skills

- [[uno-navigation-regions]] — Region concepts
- [[uno-navigation-panel-visibility]] — Visibility-based switching
