---
type: llm
---
The reply writes one XAML page meant to be shared by an Uno Material app and an Uno Simple app.
Facts the judge needs: both themes implement a shared semantic layer, so unprefixed keys (`FilledButtonStyle`, `FilledTonalButtonStyle`, `OutlinedButtonStyle`, `TextButtonStyle`, `OutlinedTextBoxStyle`, typography styles such as `HeadlineMedium`/`TitleLarge`/`BodyMedium`, brushes such as `OnSurfaceBrush`) resolve under either theme; theme-prefixed keys (`MaterialFilledButtonStyle`, `SimpleBodyMedium`) resolve only under one.
PASS if the title and helper text use semantic typography styles via `{StaticResource}`.
FAIL if the text uses literal font sizes or font families, or theme-prefixed typography keys.
