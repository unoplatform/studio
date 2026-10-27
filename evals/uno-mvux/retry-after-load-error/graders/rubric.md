---
type: llm
---
The reply shows loading, error and retry UI for an MVUX feed in an Uno Platform page.
Facts the judge needs: `mvux:FeedView` (`xmlns:mvux="using:Uno.Extensions.Reactive.UI"`) binds `Source` to the Model's `IFeed<T>`/`IState<T>` and switches between its value template, `ProgressTemplate` and `ErrorTemplate` by itself. Its built-in `Refresh` command reloads the feed, so the Model needs no retry command. The error template's DataContext is the error, not the FeedView state, so a retry button there reaches `Refresh` through the named FeedView: `Command="{Binding Refresh, ElementName=<name>}"`.
PASS if all hold:
- The forecast is an `IFeed<T>` (or `IState<T>`) on a `partial record` Model, shown in a `FeedView` with a progress template (or the default progress) and an `ErrorTemplate` with a message and a Retry button.
- Retry uses the FeedView's `Refresh` command, bound so it resolves from inside the error template (`ElementName` to the named FeedView, or an equivalent that does not rely on the error template's DataContext).
- Loading and error states are not hand-managed with `IsLoading`/`HasError` properties or try/catch in the Model.
FAIL if it adds a Model retry command or `Signal` as the only way to retry, binds Retry with a bare `{Binding Refresh}` inside `ErrorTemplate`, or builds the states with visibility converters instead of FeedView.
