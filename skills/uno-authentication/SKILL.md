---
name: uno-authentication
description: "Sign-in for Uno Platform apps with Uno.Extensions.Authentication: IAuthenticationService (LoginAsync, RefreshAsync, LogoutAsync, LoggedOut), custom login against your own API, MSAL (Microsoft Entra ID, B2C), OpenID Connect, and web/OAuth redirect providers, the access token added to Refit and Kiota calls, and the login/logout navigation flow. Use whenever an Uno app needs a login page, sign-in, sign-out, user accounts, a session that survives restarts, tokens or bearer headers on API calls, protected pages, or Microsoft, Google, Auth0, Keycloak or IdentityServer sign-in, even when the request only says 'users must log in'. Also use when the project lists Authentication, AuthenticationMsal or AuthenticationOidc in <UnoFeatures>, or when an agent is about to write its own auth service or token storage. Read this skill before writing any authentication code."
metadata:
  author: uno-platform
  category: authentication
---

# Uno Authentication

Uno.Extensions.Authentication owns the whole session: a provider signs the user in, the tokens are stored in the platform's secure storage, an HTTP handler adds the access token to API calls and refreshes it on a 401, and `IAuthenticationService.LoggedOut` reports when the session ends. Generic agents write their own `IAuthService`, keep tokens in settings or a singleton, and set `Authorization` headers by hand; each of those breaks refresh, restart, or logout. This skill routes you to the provider and the flow.

## Workflow

1. Pick the provider. A new app gets it from the template: `dotnet new unoapp -preset recommended -auth <custom|msal|oidc|Web>`, alongside the flags the `uno-build-app` skill gives. Then fix the template gaps listed in the critical rules.
2. Read the provider's reference and `references/flow.md`; add `references/http.md` when the app calls an API.
3. Ground details in the official docs: call `uno_platform_docs_search(...)`, then `uno_platform_docs_fetch(sourcePath="…")` with the `sourcePath` from a result (a relative `.md` path). Never pass a URL, `.html` link, or hand-built path.
4. Identity-provider values (client IDs, authorities, tenants, redirect URIs registered with the provider) are the developer's. Never invent them: use placeholders and report sign-in as not configured.

## Topic map

| Task | Read | Key APIs |
|------|------|----------|
| Username/password or API-key login against the app's own backend, or a local prototype check | `references/custom.md` | `AddCustom`, `AddCustom<TService>`, `Login`/`Refresh`/`Logout` callbacks |
| Sign in with Microsoft Entra ID, Microsoft accounts, or Azure AD B2C | `references/msal.md` | `AddMsal(window)`, `Scopes`, `Builder`, `InteractiveBuilder` |
| MSAL redirect URIs, Android activity, iOS URL scheme and keychain, WebAssembly `spa` registration | `references/msal-platforms.md` | `BrowserTabActivity`, `AuthenticationContinuationHelper`, `keychain-access-groups` |
| Any OpenID Connect provider (IdentityServer, Keycloak, Auth0, Okta) | `references/oidc.md` | `AddOidc`, `OidcClientOptions`, `AutoRedirectUriFromWebAuthenticationBroker` |
| A backend that redirects back with tokens in the URL (custom OAuth, social login) | `references/web.md` | `AddWeb`, `LoginStartUri`, `LogoutStartUri`, `PostLogin` |
| Start page by sign-in state, login page, logout, expired session, optional sign-in | `references/flow.md` | `RefreshAsync`, `LoggedOut`, `Qualifiers.ClearBackStack`, `initialNavigate` |
| Access token on Refit/Kiota calls, 401 refresh, cookies, reading tokens and claims | `references/http.md` | `AddRefitClient`, `AddKiotaClient`, `Headers.NoRefresh`, `ITokenCache` |

## Critical rules

- **Use `IAuthenticationService` and `ITokenCache`; build no session layer of your own.** Inject `IAuthenticationService` into Models; never create `IAuthService`, a token store, a "current user" singleton, or an `Authorization` header by hand.
- **Features:** `Authentication` (custom and web providers), `AuthenticationMsal`, or `AuthenticationOidc` in `<UnoFeatures>`. API clients need `HttpRefit` or `HttpKiota`; `Http` alone adds neither Refit nor Kiota.
- **Register in the host:** `.UseAuthentication(auth => auth.AddCustom(...))` inside `Configure`. For MSAL, OIDC and Web, the provider's `name` argument is also its configuration section (default `Msal`, `Oidc`, `Web`; the template uses `MsalAuthentication`, `OidcAuthentication`, `WebAuthentication`). `AddCustom` reads no section. `AddMsal` takes the `Window`: `.Configure((host, window) => ...)`.
- **The user is signed in while any token is stored.** A custom `Login` returns a new dictionary holding only tokens (`TokenCacheExtensions.AccessTokenKey`, ...), or `null` to fail. The template returns the `credentials` dictionary, which stores the password: replace that.
- **Pass the dispatcher:** `LoginAsync(Dispatcher, credentials)`. The MSAL provider throws without one; inject `IDispatcher` into the login Model.
- **Subscribe to `LoggedOut` once, at app lifetime, and navigate to login with `Qualifiers.ClearBackStack` there.** It fires on logout, on a failed refresh, and on an API 401 that a refresh could not fix. The recommended template does not subscribe, so logout leaves the user on the signed-in page. After `LogoutAsync`, do not navigate again.
- **Only Uno-registered clients carry the token:** `AddRefitClient`, `AddKiotaClient` or `AddClient` inside `UseHttp`. `AddHttpClient` and `new HttpClient()` send no token. Mark the token-refresh endpoint `[Headers(Uno.Extensions.Authentication.Headers.NoRefresh)]`.
- **Provider traps:** `AddWeb` without `LogoutStartUri` cannot sign out (`LogoutAsync` returns `false`). OIDC needs the `offline_access` scope, or the session ends at every restart. The template's redirect scheme `myprotocol` is a placeholder in the Android activities, iOS `Info.plist` (where it is also misplaced), `Package.appxmanifest` and `appsettings.development.json`; replace it everywhere. Tokens live in Keychain on iOS, so the app needs keychain entitlements there.

Verified against Uno.Extensions 7.4 with Uno.Sdk 6.8 and Uno.Templates 6.8. On other versions, check the template output and the docs.

## Related skills

- `uno-navigation` for routes, `initialNavigate`, and `Qualifiers.ClearBackStack` (`references/qualifiers.md`).
- `uno-mvux` for the login Model's states and commands.
- `uno-testing` to exercise sign-in, logout, and the expired-session path in the running app.
