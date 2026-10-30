---
type: llm
---
The reply fixes an Uno Platform (Uno.Extensions.Navigation) login flow where Back on the main page returns to the login page.
PASS if the call is made on an `INavigator` (injected into the model's constructor, or `this.Navigator()` on a view) with the sender first, and is awaited.
FAIL if it manipulates `Frame.BackStack` directly or navigates with `Frame.Navigate`.
