---
type: llm
---
The reply explains how to add an Uno Toolkit TabBar with Uno.Extensions navigation to an existing Uno Platform app created with `-preset blank` (single project, Uno.Sdk).
Facts the judge needs: a TabBar drives navigation as a region: `uen:Region.Attached="True"` on the TabBar (and its content area), with each `utu:TabBarItem` carrying a `uen:Region.Name` equal to a registered route (`xmlns:utu="using:Uno.Toolkit.UI"`, `xmlns:uen="using:Uno.Extensions.Navigation.UI"`).
PASS if the TabBar XAML attaches the navigation region (`uen:Region.Attached`) and gives each tab a `uen:Region.Name` that matches one of the registered routes.
FAIL if tabs switch pages through a `SelectionChanged` handler or code-behind, or the tab names do not match the routes.
