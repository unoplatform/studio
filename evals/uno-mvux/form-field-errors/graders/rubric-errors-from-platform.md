---
type: llm
---
The reply shows per-field validation errors on an MVUX edit form in an Uno Platform app.
Facts the judge needs: MVUX publishes validation results from `.Validate(...)` on a state or `.Validation(...)` on a `Command.Create` command onto the record's generated bindable, which implements `INotifyDataErrorInfo`. Uno's Input validation (`FeatureConfiguration.InputValidation.IsEnabled = true`, `input:Validation.Mode` on the input, `xmlns:input="using:Uno.UI.Xaml.Controls"`) exposes those errors on the bound input as `input:Validation.Errors`, which the page binds to show the messages.
PASS if each field's message is displayed from the input's `input:Validation.Errors` (or an `input:Validation.ErrorTemplate`) fed by MVUX's validation results.
FAIL if the reply writes its own control, converter, feed or property that reads or recomputes the errors to display them.
