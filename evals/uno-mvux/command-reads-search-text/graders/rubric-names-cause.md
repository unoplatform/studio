---
type: llm
---
The reply explains why a Button bound to an MVUX command on an Uno Platform Model stays disabled, and fixes it.
Facts the judge needs: MVUX generates a command from each public Model method. A method parameter is filled with the current value of a feed or state only when its name matches an `IFeed<T>`/`IState<T>` property case-insensitively and its type is exactly `T`; any other parameter receives the Button's `CommandParameter`. When the `CommandParameter` is null or of the wrong type, the command's CanExecute is false and the Button stays disabled.
PASS if it identifies the cause: the `text` parameter does not match the `Query` state, so it is treated as the command parameter, which is null, so the command cannot execute.
FAIL if it gives two-way binding, `UpdateSourceTrigger` or a missing `CanExecute` method as the cause (mentioning binding settings as a side check is fine), or does not connect the disabled button to the unmatched parameter.
