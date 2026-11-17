---
type: llm
---
The reply calls an MVUX model's `OpenOrder(Order order)` when a row of an Uno Platform `ListView` is tapped, without code-behind and without leaving the row selected.
Facts the judge needs: there is no trigger or event property on `CommandExtensions`.
PASS if the solution needs no code-behind and no behaviors package.
FAIL if it uses a code-behind `ItemClick`/`SelectionChanged`/`Tapped` handler or a behaviors package (`Microsoft.Xaml.Behaviors`, `EventTriggerBehavior`).
