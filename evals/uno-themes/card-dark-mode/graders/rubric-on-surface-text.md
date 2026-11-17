---
type: llm
---
The reply rewrites an Uno Platform card so it renders correctly in Light and Dark under the app's Uno Themes design theme (Simple, from the recommended template).
Facts the judge needs: the semantic brushes (`SurfaceBrush`, `OnSurfaceBrush`, `OnSurfaceVariantBrush`, `OutlineVariantBrush`, `BackgroundBrush`, ...) resolve under both Material and Simple; brushes must be referenced with `{ThemeResource}` so they re-resolve on a Light/Dark switch.
PASS if the text uses the paired `On` role through `{ThemeResource ...}`: `OnSurfaceBrush` for the title, `OnSurfaceBrush` or `OnSurfaceVariantBrush` for the secondary line.
FAIL if the text keeps a hex color or uses a WinUI Fluent key such as `TextFillColorPrimaryBrush`.
