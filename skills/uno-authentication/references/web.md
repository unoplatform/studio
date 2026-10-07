# Web Authentication (browser redirect)

`AddWeb` opens a login URL in the browser and reads the tokens from the URL the browser is redirected to. Use it for a backend that runs its own OAuth or social-login flow and redirects back with `access_token` (and optionally `refresh_token`) in the query or fragment. For a standard OpenID Connect provider use `AddOidc` (`references/oidc.md`) instead.

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

- `PostLogin` gets the tokens already extracted from the callback URL. Use it to exchange a code for tokens or to add an `IdToken`; only the `AddWeb<TService>` overload also receives the callback URL itself.
- Without `Refresh`, `RefreshAsync` keeps the stored tokens until an API returns 401. A refresh call to the backend must carry the no-refresh header (`references/http.md`).

## Platforms

- **Android**: the template's `Platforms/Android/WebAuthenticationBrokerActivity.Android.cs` has the placeholder scheme `myprotocol`: set `DataScheme` to the callback URI's scheme.
- **iOS and macOS**: the callback must use a custom scheme registered under `CFBundleURLTypes` in `Info.plist`. The template's top-level `CFBundleURLSchemes` key is ignored by iOS; move its scheme into `CFBundleURLTypes`.
- **WebAssembly**: the callback must be on the app's own origin; use the URL `WebAuthenticationBroker.GetCurrentApplicationCallbackUri()` returns (`<origin>/authentication-callback` by default) and register it with the backend.
- **Windows (WinAppSDK)**: `AddWeb` opens an out-of-process browser instead of the `WebAuthenticationBroker` and handles the redirect activation itself.
