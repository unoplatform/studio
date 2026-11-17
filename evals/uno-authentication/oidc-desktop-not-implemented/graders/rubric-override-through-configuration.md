---
type: llm
---
The reply gives the Skia desktop head of an Uno Platform app its own OIDC redirect URI, while every head reads the same `OidcAuthentication` section of appsettings.json.
Facts the judge needs: any code option given to `AddOidc` (`.RedirectUri(...)`, `.Authority(...)`, ...) makes the provider ignore the configuration section entirely (`Settings.Options ?? Configuration.Get(name)`), so a code override of one value drops the authority, client ID and scopes. A per-head override of one value goes through configuration: for example `AddInMemoryCollection` entries for `OidcAuthentication:RedirectUri` and `OidcAuthentication:PostLogoutRedirectUri` in a `ConfigureAppConfiguration` call made in the desktop head after the embedded appsettings. `AutoRedirectUriFromWebAuthenticationBroker()` is not a code option of that kind.
PASS if the desktop head's redirect URI is overridden through configuration, not with an `AddOidc` code option.
FAIL if it overrides the redirect URI with a code option such as `.RedirectUri(...)` on `AddOidc`, or gives the desktop head no override of its own.
