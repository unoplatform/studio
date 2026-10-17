# Uno Navigation ContentControl

## Key Principles (Stable)

- ContentControl can be a navigation target region
- Content is replaced, not pushed. A route whose view is a `Page` is wrapped in a `FrameView`, so `-` still pops within that pane
- Use `uen:Region.Attached="True"` on the ContentControl
- Good for sidebar detail panes, settings containers, or single-area content swaps

## Docs, for what this page does not cover

Use these when the sections above do not answer the question: search, then fetch only the section you need, with the `anchor` from the search result. A whole page stays in context for the rest of the session.

### Fetch the ContentControl Navigation Documentation

```
uno_platform_docs_search("Uno Navigation ContentControl region content switching")
```

Primary documentation pages:
- **Define Regions**: `external/uno.extensions/doc/Learn/Navigation/Walkthrough/DefineRegions.md`
- **Navigation Region Reference**: `external/uno.extensions/doc/Reference/Navigation/NavigationRegion.md`

Fetch the regions reference:

```
uno_platform_docs_fetch(sourcePath="external/uno.extensions/doc/Reference/Navigation/NavigationRegion.md", anchor="…")
```

### For Alternative Approaches

If the user needs visibility-based switching instead:

See the `references/panel-visibility.md`.

## Related Skills

- `references/regions.md` — Region concepts
- `references/panel-visibility.md` — Visibility-based switching
