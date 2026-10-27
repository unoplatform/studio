---
type: llm
---
The reply calls an MVUX model's `OpenOrder(Order order)` when a row of an Uno Platform `ListView` is tapped, without code-behind and without leaving the row selected.
Background (Uno Toolkit docs, CommandExtensions): `utu:CommandExtensions.Command="{Binding OpenOrder}"` on a `ListView` fires on item click and passes the clicked item when `CommandParameter` is not set. The `ListView` must have `IsItemClickEnabled="True"` or the command never fires. MVUX generates a command named after the public method (`OpenOrder`). There is no trigger/event property on `CommandExtensions`.
PASS if all hold:
- The `ListView` uses `utu:CommandExtensions.Command` (namespace `using:Uno.Toolkit.UI`) bound to the generated `OpenOrder` command, with the clicked order passed (default parameter or an explicit equivalent).
- The `ListView` sets `IsItemClickEnabled="True"` and `SelectionMode="None"` (so no row stays selected).
- No code-behind `ItemClick`/`SelectionChanged`/`Tapped` handler, behaviors package (`Microsoft.Xaml.Behaviors`, `EventTriggerBehavior`), or invented property such as `CommandExtensions.CommandTrigger` is part of the solution.
FAIL if `IsItemClickEnabled="True"` is missing, if the answer needs code-behind or a behaviors package, or if it uses a property that does not exist on `CommandExtensions`.
