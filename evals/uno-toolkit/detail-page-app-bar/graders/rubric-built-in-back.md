---
type: llm
---
The reply builds the top bar of an Uno Platform detail page: title, back arrow to the previous page, and a Share action bound to `ShareCommand`.
Facts the judge needs: `utu:NavigationBar` with the default `MainCommandMode="Back"` navigates back by itself (it calls `Frame.GoBack()` / handles back requests) when its `MainCommand` is clicked. Binding a back/go-back command on the `MainCommand` in that mode navigates back twice. A custom `Command` on `MainCommand` belongs only with `MainCommandMode="Action"`.
PASS if the back arrow relies on the bar's built-in back behavior: `MainCommand` is omitted, or set only to change the icon or label, with no `Command` bound on it while `MainCommandMode` is `Back` (the default).
FAIL if `MainCommand` has a `Command` (or `Click` handler) that navigates back while `MainCommandMode` is `Back` or unset.
