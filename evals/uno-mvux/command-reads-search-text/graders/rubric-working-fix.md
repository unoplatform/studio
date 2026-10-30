---
type: llm
---
The reply explains why a Button bound to an MVUX command on an Uno Platform Model stays disabled, and fixes it.
Facts the judge needs: MVUX generates a command from each public Model method. A method parameter is filled with the current value of a feed or state only when its name matches an `IFeed<T>`/`IState<T>` property case-insensitively and its type is exactly `T`; any other parameter receives the Button's `CommandParameter`. When the `CommandParameter` is null or of the wrong type, the command's CanExecute is false and the Button stays disabled. The fixes the docs support: rename the parameter to match (`string query`), map it with `[FeedParameter(nameof(Query))]`, bind `CommandParameter` to `Query`, or drop the parameter and read the state inside the method with `await Query`.
PASS if it gives one of the working fixes above (any one passes).
FAIL if none of its fixes would make the command executable.
