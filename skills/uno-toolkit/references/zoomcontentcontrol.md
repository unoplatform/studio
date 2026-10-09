# Uno Toolkit ZoomContentControl

## Workflow

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

- The fit-to-available-size property is **`AutoFitToCanvas`** (boolean). There is **no `AutoFit`** property.
- Programmatic zoom sets the `ZoomLevel` dependency property (bindable, clamped by `MinZoomLevel`/`MaxZoomLevel`). Methods are `FitToCanvas()`, `CenterContent()`, `ResetZoom()`, `ResetScroll()`, `ResetViewport()`. There is **no `ZoomTo`, `ZoomToRect`, or `Zoom`**.

## Key Principles (Stable)

- `MinZoomLevel` and `MaxZoomLevel` — zoom range
- `ZoomLevel` — current zoom (bindable, two-way)
- `IsZoomAllowed` — enable/disable zoom
- `IsPanAllowed` — enable/disable pan
- `AutoFitToCanvas` — fit content to available size
- Supports mouse wheel and pinch-to-zoom gestures
- XAML namespace: `xmlns:utu="using:Uno.Toolkit.UI"`

## Related Skills

- (standalone control, no primary dependencies)
