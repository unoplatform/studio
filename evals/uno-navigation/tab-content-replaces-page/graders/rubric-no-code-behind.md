---
type: llm
---
The reply diagnoses and fixes an Uno.Extensions.Navigation TabBar shell where tapping a tab replaces the whole page.
PASS if the fix stays in the XAML regions.
FAIL if it toggles page visibility or swaps content in code-behind (`SelectionChanged` handlers, `Frame.Navigate`).
