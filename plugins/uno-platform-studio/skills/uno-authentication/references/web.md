# Web Authentication (browser redirect)

`AddWeb` opens a login URL in the browser and reads the tokens from the URL the browser is redirected to. Use it for a backend that runs its own OAuth or social-login flow and redirects back with `access_token` (and optionally `refresh_token`) in the query or fragment. For a standard OpenID Connect provider use `AddOidc` (`references/oidc.md`) instead. Verified with Uno.Samples `UI/Authentication.WebExtensionsDemo`, backend included (desktop, WebAssembly, Android; Uno.Sdk 7.0.0-dev.115).

Feature: `Authentication` in `<UnoFeatures>` (Web is part of it). New app: `dotnet new unoapp -preset recommended -auth Web` (capital `W`).

## Registration and configuration

```csharp
.UseAuthentication(auth => auth.AddWeb(name: "WebAuthentication"))
```

The `name` is the configuration section (default `Web`; the template uses `WebAuthentication`):

```json
"WebAuthentication": {
  "LoginStartUri": "https://api.example.com/auth/login?redirect_uri=myapp%3A%2F%2Fcallback",
  "LoginCallbackUri": "myapp://callback",
  "LogoutStartUri": "https://api.example.com/auth/logout?redirect_uri=myapp%3A%2F%2Fcallback",
  "LogoutCallbackUri": "myapp://callback"
}
```

| Key | Meaning |
|---|---|
| `LoginStartUri` | Page opened to sign in. Required: without it `LoginAsync` returns `false` |
| `LoginCallbackUri` | Redirect that ends the sign-in. Optional when `LoginStartUri` carries a `redirect_uri` query parameter, which is used instead |
| `LogoutStartUri` | Page opened to sign out. **Required for logout**: without it `LogoutAsync` returns `false`, the tokens stay, and `LoggedOut` never fires |
| `LogoutCallbackUri` | Redirect that ends the sign-out |
| `AccessTokenKey`, `RefreshTokenKey` | Names of the token parameters in the callback URL. Default `access_token` and `refresh_token` |
| `PrefersEphemeralWebBrowserSession` | iOS 13+ only: do not share cookies with Safari |

Values that depend on runtime state go in code:

```csharp
auth.AddWeb(web => web
    .PrepareLoginStartUri((sp, tokenCache, credentials, loginStartUri, ct) =>
        ValueTask.FromResult($"{loginStartUri}&provider={credentials?["Provider"]}"))
    .PostLogin((sp, tokenCache, credentials, extracted, ct) =>
        ValueTask.FromResult<IDictionary<string, string>?>(extracted)))
```

| Call | Callback receives (then a `CancellationToken`) | Returns |
|---|---|---|
| `PrepareLoginStartUri` | `()`, `(credentials)`, `(sp)`, `(sp, credentials)`, `(sp, tokenCache, credentials, loginStartUri)` | the URL to open |
| `PostLogin` | `(tokens)` or `(sp, tokenCache, credentials, tokens)`; with `AddWeb<TService>`, `(service, tokens)` or `(service, sp, tokenCache, credentials, callbackUrl, tokens)` | the tokens to store, or `null` to fail |
| `Refresh` | `(tokens)` or `(sp, tokenCache, tokens)`; with `AddWeb<TService>`, `(service, tokens)` or `(service, sp, tokenCache, tokens)` | the renewed tokens, or `null` to sign out |

`PrepareLoginCallbackUri`, `PrepareLogoutStartUri` and `PrepareLogoutCallbackUri` follow `PrepareLoginStartUri`.

- `PostLogin` gets the tokens already extracted from the callback URL. Use it to exchange a code for tokens or to add an `IdToken`; only the `AddWeb<TService>` overload also receives the callback URL itself, so a `state` check needs that overload.
- Without `Refresh`, `RefreshAsync` reports success while any token is stored; the session ends only on an API 401. In `Refresh`, return `null` only when the backend rejects the refresh token (the service clears the tokens and raises `LoggedOut`). An exception (backend unreachable) comes out of `RefreshAsync` with the tokens kept, and the template's `InitialNavigate` does not catch it: wrap the startup call. The refresh endpoint must carry the no-refresh header (`references/http.md`).
- `LogoutAsync` clears the tokens whatever the browser returned, a cancelled or timed-out page included; only a missing `LogoutStartUri` makes it return `false`.

## Platforms

- **Android**: the template's `Platforms/Android/WebAuthenticationBrokerActivity.Android.cs` has the placeholder scheme `myprotocol`: set `DataScheme` to the callback URI's scheme.
- **iOS and macOS**: the callback must use a custom scheme registered under `CFBundleURLTypes` in `Info.plist`. The template's top-level `CFBundleURLSchemes` key is ignored by iOS; move its scheme into `CFBundleURLTypes`.
- **WebAssembly**: unlike `AddOidc`, the provider sends the configured callback here too, and it must be on the app's own origin: set the `redirect_uri` to `WebAuthenticationBroker.GetCurrentApplicationCallbackUri()` (`<origin>/authentication-callback`) through configuration at startup (`AddInMemoryCollection` in `ConfigureAppConfiguration`), register it with the backend, and serve `index.html` for that path.
- **Desktop (Skia)**: since Uno.Extensions 8.0.0-dev.135 `AddWeb` registers a loopback `WebAuthenticationBroker` (`DesktopWebAuthenticationBrokerProvider`): `LoginCallbackUri` and `LogoutCallbackUri` must be loopback HTTP addresses such as `http://localhost:<port>/authentication-callback`, registered with the backend as is. The listener completes on the first request to that path from any local page or process, and the Web provider does not tie it to the flow it started: send a `state` from `PrepareLoginStartUri` and check it in `PostLogin` before accepting the tokens. Tokens in the fragment work: the broker relays them. The version gate, the opt-in and the ordering rule (first registration wins; touch nothing on `WebAuthenticationBroker` before `UseAuthentication`) are in `references/oidc-desktop.md`.
- **Windows (WinAppSDK)**: `AddWeb` opens an out-of-process browser instead of the `WebAuthenticationBroker` and handles the redirect activation itself.

Docs: `uno_platform_docs_fetch(sourcePath="external/uno.extensions/doc/Learn/Authentication/HowTo-WebAuthentication.md")` and the walkthrough `external/uno.extensions/doc/Learn/Walkthrough/Authentication.Web.howto.md`.
