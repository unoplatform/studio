---
type: llm
---
The reply gives XAML for a destructive button and an error-state TextBox in an Uno Platform app on the Simple theme (the recommended template's default theme).
Facts the judge needs: Simple ships `SimpleDangerPrimaryButtonStyle` (filled destructive) and `SimpleDangerSubtleButtonStyle` (text destructive) for buttons, and `SimpleTextBoxErrorStyle` for a TextBox in an error state; the shared semantic error roles are `ErrorBrush`/`OnErrorBrush` and `ErrorContainerBrush`/`OnErrorContainerBrush`, referenced with `{ThemeResource}`. Overriding a lightweight styling key to alias an error brush (for example `<StaticResource x:Key="FilledButtonBackground" ResourceKey="ErrorBrush" />`) is also acceptable.
PASS if the delete button uses a Simple danger style, or the error role brushes (background `ErrorBrush` with foreground `OnErrorBrush`) through `{ThemeResource}`.
FAIL if it copies a full control template for the button, or uses Material-only keys.
