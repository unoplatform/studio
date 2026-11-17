---
type: llm
---
The reply writes one XAML page meant to be shared by an Uno Material app and an Uno Simple app.
Facts the judge needs: both themes implement a shared semantic layer, so unprefixed keys (`FilledButtonStyle`, `FilledTonalButtonStyle`, `OutlinedButtonStyle`, `TextButtonStyle`, `OutlinedTextBoxStyle`, typography styles such as `HeadlineMedium`/`TitleLarge`/`BodyMedium`, brushes such as `OnSurfaceBrush`) resolve under either theme; theme-prefixed keys (`MaterialFilledButtonStyle`, `SimpleBodyMedium`) resolve only under one.
PASS if the same page XAML works under both themes without switching.
FAIL if it uses WinUI Fluent keys like `AccentButtonStyle` as the portable answer, or proposes per-app XAML or code that switches styles by theme.
