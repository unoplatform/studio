---
type: llm
---
The reply builds the top bar of an Uno Platform detail page: title, back arrow to the previous page, and a Share action bound to `ShareCommand`.
Background (Uno Toolkit docs): `utu:NavigationBar` with the default `MainCommandMode="Back"` navigates back by itself (it calls `Frame.GoBack()` / handles back requests) when its `MainCommand` is clicked. Binding a back/go-back command on the `MainCommand` in that mode navigates back twice. A custom `Command` on `MainCommand` belongs only with `MainCommandMode="Action"`.
PASS if all hold:
- The bar is `utu:NavigationBar` with the title as its `Content`.
- The back arrow relies on the bar's built-in back behavior: `MainCommand` is omitted, or set only to change the icon/label, with no `Command` bound on it while `MainCommandMode` is `Back` (the default).
- Share is an `AppBarButton` in `PrimaryCommands` (or as direct child content, which is `PrimaryCommands`) with `Command` bound to `ShareCommand`.
FAIL if `MainCommand` has a `Command` (or `Click` handler) that navigates back while `MainCommandMode` is `Back` or unset, if it invents a type such as `NavigationBarMainCommand`, or if it builds the bar from a `Grid`/`CommandBar` instead of `NavigationBar`.
