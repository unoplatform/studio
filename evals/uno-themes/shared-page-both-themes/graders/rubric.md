---
type: llm
---
The reply writes one XAML page meant to be shared by an Uno Material app and an Uno Simple app.
Uno Themes facts the judge needs: both themes implement a shared semantic layer, so unprefixed keys (`FilledButtonStyle`, `FilledTonalButtonStyle`, `OutlinedButtonStyle`, `TextButtonStyle`, `OutlinedTextBoxStyle`, typography styles such as `HeadlineMedium`/`TitleLarge`/`BodyMedium`, brushes such as `OnSurfaceBrush`) resolve under either theme; theme-prefixed keys (`MaterialFilledButtonStyle`, `SimpleBodyMedium`) resolve only under one. `OutlinedTextBoxStyle` is already the implicit TextBox style in both themes, so leaving the TextBox unstyled is fine. There is no shared semantic danger button style: `SimpleDangerPrimaryButtonStyle` exists only under Simple, so a portable red button uses the error roles, e.g. a semantic button style with its lightweight keys (`FilledButtonBackground`, `FilledButtonForeground`, ...) overridden in `Button.Resources` to `{ThemeResource ErrorBrush}` / `{ThemeResource OnErrorBrush}`, or Background/Foreground set to those brushes.
PASS if all hold:
- Save uses `FilledButtonStyle` and Cancel uses a lower-emphasis semantic button style (`FilledTonalButtonStyle`, `OutlinedButtonStyle`, or `TextButtonStyle`), via `{StaticResource}`.
- The title and helper text use semantic typography styles via `{StaticResource}`; any brushes are semantic brushes via `{ThemeResource}`.
- The Delete button gets its red from the error roles (`ErrorBrush` with `OnErrorBrush`) through `{ThemeResource}`, not from a Simple-only or Material-only style.
- No hard-coded colors, font sizes, or font families.
FAIL if it uses theme-prefixed style keys, hard-codes colors or font sizes, uses WinUI Fluent keys like `AccentButtonStyle` as the portable answer, or proposes per-app XAML or code that switches styles by theme.
