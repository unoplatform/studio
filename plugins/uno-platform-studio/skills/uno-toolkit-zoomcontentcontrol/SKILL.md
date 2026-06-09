---
name: uno-toolkit-zoomcontentcontrol
description: "Zoomable and pannable container for images, maps, diagrams, or document viewers."
when_to_use: "Use when zoom and pan are needed for images, maps, diagrams, or document viewers — mouse wheel zoom, middle-click pan, pinch-to-zoom, configurable min/max zoom levels, and programmatic control."
metadata:
  author: uno-platform
  version: "2.5"
  category: toolkit
---

# Uno Toolkit ZoomContentControl — Agent Skill

## Workflow

> **Docs lookup:** call `uno_platform_docs_search(...)` first, then `uno_platform_docs_fetch(sourcePath="…")` using the `sourcePath` field from a result (a relative `.md` path; add the result's `anchor` for a section). Never pass a URL, a `.html` link, or a hand-built path.

### Step 1: Fetch the ZoomContentControl Documentation

```
uno_platform_docs_search("Uno Toolkit ZoomContentControl zoom pan pinch mouse wheel")
```

Primary documentation page:
- **ZoomContentControl**: `external/uno.toolkit.ui/doc/controls/ZoomContentControl.md`

Fetch the page:

```
uno_platform_docs_fetch(sourcePath="external/uno.toolkit.ui/doc/controls/ZoomContentControl.md")
```

## Critical Rules

- The fit-to-available-size property is **`AutoFit`** (boolean). There is **no `AutoFitToCanvas`** property — that name does not exist on `ZoomContentControl`.
- Programmatic zoom uses methods `ZoomTo(float)` and `ZoomToRect(Rect)` on the control instance — not a settable `Zoom` property.

## Key Principles (Stable)

- `MinZoomLevel` and `MaxZoomLevel` — zoom range
- `ZoomLevel` — current zoom (bindable, two-way)
- `IsZoomAllowed` — enable/disable zoom
- `IsPanAllowed` — enable/disable pan
- `AutoFit` — fit content to available size
- Supports mouse wheel, middle-click pan, pinch-to-zoom gestures
- XAML namespace: `xmlns:utu="using:Uno.Toolkit.UI"`

## Related Skills

- (standalone control, no primary dependencies)
