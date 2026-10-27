---
type: llm
---
The reply fixes an Uno Platform (Uno.Extensions.Navigation) login flow where Back on the main page returns to the login page.
PASS if all hold:
- The navigation after sign-in clears the back stack with a navigation qualifier: the `-/` prefix on the route (for example `NavigateRouteAsync(this, "-/Main")` or `uen:Navigation.Request="-/Main"`) or `Qualifiers.ClearBackStack` passed as the qualifier argument of an `INavigator` call.
- The call is made on an `INavigator` (injected into the model's constructor, or `this.Navigator()` on a view) with the sender first, and is awaited.
FAIL if it manipulates `Frame.BackStack` directly, swallows the Android back button with a `BackRequested` handler instead of fixing the navigation, or swaps `Window.Content`/the root frame by hand.
