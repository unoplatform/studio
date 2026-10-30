---
type: llm
---
The reply writes one XAML page meant to be shared by an Uno Material app and an Uno Simple app.
Facts the judge needs: both themes implement a shared semantic layer, so unprefixed keys (`FilledButtonStyle`, `FilledTonalButtonStyle`, `OutlinedButtonStyle`, `TextButtonStyle`, `OutlinedTextBoxStyle`, typography styles such as `HeadlineMedium`/`TitleLarge`/`BodyMedium`, brushes such as `OnSurfaceBrush`) resolve under either theme; theme-prefixed keys (`MaterialFilledButtonStyle`, `SimpleBodyMedium`) resolve only under one. There is no shared semantic danger button style: `SimpleDangerPrimaryButtonStyle` exists only under Simple, so a portable red button uses the error roles, for example a semantic button style with its lightweight keys (`FilledButtonBackground`, `FilledButtonForeground`, ...) overridden in `Button.Resources` to `{ThemeResource ErrorBrush}` / `{ThemeResource OnErrorBrush}`, or Background/Foreground set to those brushes.
PASS if the Delete button gets its red from the error roles (`ErrorBrush` with `OnErrorBrush`) through `{ThemeResource}`.
FAIL if the red comes from a Simple-only or Material-only style, or from a literal color.
