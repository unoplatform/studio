---
type: llm
---
The reply shows a "fix the highlighted fields" line on an MVUX edit form in an Uno Platform app.
Facts the judge needs: the generated bindable of the edited record exposes a public `HasErrors` property that is true while MVUX validation results exist, so the line binds to it, for example `Visibility="{Binding Draft.HasErrors}"`.
PASS if the summary line's visibility comes from the generated `HasErrors` of the edited record or model.
FAIL if it comes from a validity feed, a converter that inspects the errors, or code that runs the validator again.
