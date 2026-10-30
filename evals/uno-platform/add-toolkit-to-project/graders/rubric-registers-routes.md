---
type: llm
---
The reply explains how to add an Uno Toolkit TabBar with Uno.Extensions navigation to an existing Uno Platform app created with `-preset blank` (single project, Uno.Sdk).
PASS if it wires navigation in App.xaml.cs: the host builder calls `.UseNavigation(...)` and registers the tab pages' routes (`ViewMap` plus `RouteMap`).
FAIL if it registers no routes, or switches pages with `Frame.Navigate` or code-behind instead of Uno.Extensions navigation.
