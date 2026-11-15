---
type: llm
---
The reply gives XAML for a destructive button and an error-state TextBox in an Uno Platform app on the Simple theme (the recommended template's default theme).
Facts the judge needs: Simple ships `SimpleDangerPrimaryButtonStyle` (filled destructive) and `SimpleDangerSubtleButtonStyle` (text destructive) for buttons, and `SimpleTextBoxErrorStyle` for a TextBox in an error state; the shared semantic error roles are `ErrorBrush`/`OnErrorBrush` and `ErrorContainerBrush`/`OnErrorContainerBrush`, referenced with `{ThemeResource}`. Overriding a lightweight styling key to alias an error brush (for example `<StaticResource x:Key="FilledButtonBackground" ResourceKey="ErrorBrush" />`) is also acceptable.
PASS if the TextBox error look uses `SimpleTextBoxErrorStyle` (switched on when the emails don't match, for example via a binding, a visual state or code) or error-role brushes.
FAIL if the TextBox error look uses literal colors, a copied control template, or Material-only keys.
