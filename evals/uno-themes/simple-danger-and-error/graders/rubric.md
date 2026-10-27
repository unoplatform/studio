---
type: llm
---
The reply gives XAML for a destructive button and an error-state TextBox in an Uno Platform app on the Simple theme (the recommended template's default theme).
Uno Themes facts the judge needs: Simple ships `SimpleDangerPrimaryButtonStyle` (filled destructive) and `SimpleDangerSubtleButtonStyle` (text destructive) for buttons, and `SimpleTextBoxErrorStyle` for a TextBox in an error state; the shared semantic error roles are `ErrorBrush`/`OnErrorBrush` and `ErrorContainerBrush`/`OnErrorContainerBrush`, referenced with `{ThemeResource}`. Overriding a lightweight styling key (e.g. a `SolidColorBrush` keyed `FilledButtonBackground`) set to an error brush is also acceptable.
PASS if all hold:
- The delete button uses a Simple danger style, or the error role brushes (background `ErrorBrush` with foreground `OnErrorBrush`) through `{ThemeResource}`.
- The TextBox error look uses `SimpleTextBoxErrorStyle` (switched on when the emails don't match, e.g. via a binding, visual state, or code) or error-role brushes, not literal colors.
FAIL if it hard-codes red/hex colors on the controls, copies a full control template, or uses Material-only keys.
