---
type: llm
---
The reply rewrites an Uno Platform card so it renders correctly in Light and Dark under the app's Uno Themes design theme (Simple, from the recommended template).
Uno Themes facts the judge needs: the semantic brushes (`SurfaceBrush`, `OnSurfaceBrush`, `OnSurfaceVariantBrush`, `OutlineVariantBrush`, `BackgroundBrush`, ...) resolve under both Material and Simple; brushes must be referenced with `{ThemeResource}` so they re-resolve on a Light/Dark switch; typography styles (`TitleMedium`, `BodyMedium`, ...) are referenced with `{StaticResource}`.
PASS if all hold:
- Every hard-coded color is replaced with a semantic brush through `{ThemeResource ...}`; the background uses a surface/background role and the text uses its paired `On` role (`OnSurfaceBrush` for the title, `OnSurfaceBrush` or `OnSurfaceVariantBrush` for the secondary line); the border uses an outline role.
- Literal `FontSize`/`FontWeight` are replaced with semantic typography styles (e.g. `TitleMedium`/`TitleLarge`, `BodyMedium`/`BodySmall`) applied with `Style="{StaticResource ...}"`.
FAIL if it keeps hex colors in the card, references a brush with `{StaticResource}`, defines its own Light/Dark ThemeDictionaries with new hex values instead of using the theme's brushes, or uses only WinUI Fluent keys (`TextFillColorPrimaryBrush`, `CardBackgroundFillColorDefaultBrush`) instead of the Uno Themes semantic brushes.
