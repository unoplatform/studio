# Uno Navigation ContentControl

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

See the `references/panel-visibility.md`.

## Key Principles (Stable)

- ContentControl can be a navigation target region
- No back stack — content simply replaced
- Use `uen:Region.Attached="True"` on the ContentControl
- Good for sidebar detail panes, settings containers, or single-area content swaps

## Related Skills

- `references/regions.md` — Region concepts
- `references/panel-visibility.md` — Visibility-based switching
