---
type: llm
---
The reply sets up sign-in for an Uno Platform app against the app's own login API with Uno.Extensions.Authentication.
Facts the judge needs: with `AddCustom`, whatever dictionary the `Login` callback returns is stored as-is, every key, and the user counts as signed in while any entry is stored. The template's callback returns the `credentials` dictionary it received, which stores the password. The token keys are `TokenCacheExtensions.AccessTokenKey`, `RefreshTokenKey` and `IdTokenKey`.
PASS if the `Login` callback returns a new dictionary holding only tokens from the login response.
FAIL if it returns the `credentials` dictionary, or any dictionary that keeps the username or the password.
