---
type: llm
---
The reply fixes an Uno Platform (Uno.Extensions.Navigation) login flow where Back on the main page returns to the login page.
PASS if the navigation after sign-in clears the back stack with a navigation qualifier: the `-/` prefix on the route (for example `NavigateRouteAsync(this, "-/Main")` or `uen:Navigation.Request="-/Main"`) or `Qualifiers.ClearBackStack` passed as the qualifier argument of an `INavigator` call.
FAIL if it swallows the Android back button with a `BackRequested` handler instead of fixing the navigation, or swaps `Window.Content` or the root frame by hand.
