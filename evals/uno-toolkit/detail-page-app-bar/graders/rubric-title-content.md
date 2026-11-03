---
type: llm
---
The reply builds the top bar of an Uno Platform detail page: title, back arrow to the previous page, and a Share action bound to `ShareCommand`.
PASS if the bar is a `utu:NavigationBar` with the title as its `Content`.
FAIL if it builds the bar from a `Grid` or `CommandBar` instead of `NavigationBar`.
