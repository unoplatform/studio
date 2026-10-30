---
type: llm
---
The reply rewrites an Uno Platform card so it renders correctly in Light and Dark under the app's Uno Themes design theme (Simple, from the recommended template).
Facts the judge needs: typography styles (`TitleMedium`, `BodyMedium`, ...) are referenced with `{StaticResource}`.
PASS if literal `FontSize`/`FontWeight` are replaced with semantic typography styles (for example `TitleMedium`/`TitleLarge`, `BodyMedium`/`BodySmall`) applied with `Style="{StaticResource ...}"`.
FAIL if the card keeps literal `FontSize` or `FontWeight` values.
