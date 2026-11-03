---
type: llm
---
The reply fixes an Uno Platform app (Uno.Extensions.Authentication and Navigation) whose sign-out and expired session leave the user on the signed-in page.
Facts the judge needs: `IAuthenticationService.LoggedOut` is raised whenever the stored tokens are cleared: an explicit `LogoutAsync`, a refresh that fails, and an API 401 that a refresh could not fix. The recommended template subscribes nowhere. The `InitialNavigate` callback in `App.xaml.cs`, and a Shell model's constructor, live as long as the app.
PASS if the app subscribes to `LoggedOut` once, in a place that lives as long as the app (the `InitialNavigate` callback, a Shell model, or `App` itself).
FAIL if it subscribes in a page or a page's Model that is created on each navigation, or adds no `LoggedOut` handler.
