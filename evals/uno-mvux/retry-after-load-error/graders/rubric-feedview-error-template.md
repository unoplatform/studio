---
type: llm
---
The reply shows loading, error and retry UI for an MVUX feed in an Uno Platform page.
Facts the judge needs: `mvux:FeedView` (`xmlns:mvux="using:Uno.Extensions.Reactive.UI"`) binds `Source` to the Model's `IFeed<T>`/`IState<T>` and switches between its value template, `ProgressTemplate` and `ErrorTemplate` by itself. Its built-in `Refresh` command reloads the feed, so the Model needs no retry command. The error template's DataContext is the error, not the FeedView state, so a retry button there reaches `Refresh` through the named FeedView: `Command="{Binding Refresh, ElementName=<name>}"`.
PASS if the forecast is an `IFeed<T>` (or `IState<T>`) on a `partial record` Model, shown in a `FeedView` whose `ErrorTemplate` has a message and a Retry button.
FAIL if it builds the loading and error states with visibility converters instead of FeedView.
