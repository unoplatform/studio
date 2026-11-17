---
type: llm
---
The reply sets up sign-in for an Uno Platform app against the app's own login API with Uno.Extensions.Authentication.
Facts the judge needs: `UseAuthentication` stores the tokens in the platform's secure storage, an HTTP handler adds the access token to clients registered through Uno (`AddRefitClient`, `AddKiotaClient`, `AddClient` inside `UseHttp`) and refreshes it on a 401, Models inject `IAuthenticationService` to sign in and out, and `ITokenCache` reads the stored tokens.
PASS if the reply keeps the tokens only where Uno.Extensions.Authentication stores them, with no auth service, token store or current-user holder of its own.
FAIL if it writes its own `IAuthService`, token store or "current user" singleton, or keeps tokens in settings, files or a static field.
