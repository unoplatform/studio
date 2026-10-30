---
type: llm
---
The reply adds a Customers page to an Uno Platform app that already uses CommunityToolkit.Mvvm.
PASS if it keeps the page on MVVM and does not suggest migrating the page or the app to MVUX or another presentation framework.
FAIL if it converts the page to MVUX (a `partial record ...Model`, `IFeed`/`IListFeed`/`IState`, `FeedView`) or another framework, or recommends migrating.
