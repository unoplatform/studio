---
type: llm
---
The reply shows a Delete/Cancel confirmation from an MVUX model in an Uno Platform app with Uno.Extensions.Navigation and acts on the answer.
PASS if all hold:
- The model gets an `INavigator` by constructor injection and calls the generic `ShowMessageDialogAsync<T>(this, title: ..., content: ..., buttons: ...)` (sender first) with `DialogAction` buttons, awaiting it; the non-generic overload is not used to read the choice (it returns no result).
- The result is compared with the chosen action's `Id` (or its `Label` when no `Id` is set), and the delete runs only for the Delete choice.
- Alternatively, it navigates to a registered `ContentDialog` route (or with the `!`/`Qualifiers.Dialog` qualifier) for result and checks the returned value; that also passes if it is wired correctly.
FAIL if it constructs a `ContentDialog` or `MessageDialog` in the model and sets `XamlRoot`, reaches into the page/window from the model, or ignores the user's choice.
