---
type: llm
---
The reply calls an MVUX model's `OpenOrder(Order order)` when a row of an Uno Platform `ListView` is tapped, without code-behind and without leaving the row selected.
Facts the judge needs: `utu:CommandExtensions.Command="{Binding OpenOrder}"` on a `ListView` fires on item click and passes the clicked item when `CommandParameter` is not set. MVUX generates a command named after the public method (`OpenOrder`).
PASS if the `ListView`'s `utu:CommandExtensions.Command` (namespace `using:Uno.Toolkit.UI`) is bound to the generated `OpenOrder` command, with the clicked order passed (the default parameter or an explicit equivalent).
FAIL if the command is bound to something else, or the clicked order is not passed.
