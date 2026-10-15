# OpenID Connect (OIDC)

`AddOidc` signs users in with any OpenID Connect identity provider (Duende IdentityServer, Keycloak, Auth0, Okta, Entra ID through its OIDC endpoints) using `Duende.IdentityModel.OidcClient`. The login page is a single "Sign in" button: the provider's page opens in the system browser and redirects back to the app.

Feature: `AuthenticationOidc` in `<UnoFeatures>`. New app: `dotnet new unoapp -preset recommended -auth oidc`.

## Registration and configuration

```csharp
.UseAuthentication(auth => auth.AddOidc(name: "OidcAuthentication"))
```

The `name` is the configuration section (default `Oidc`; the template uses `OidcAuthentication`). It binds to `OidcClientOptions`:

```json
"OidcAuthentication": {
  "Authority": "https://demo.duendesoftware.com/",
  "ClientId": "interactive.public",
  "Scope": "openid profile email api offline_access",
  "RedirectUri": "myapp://callback",
  "PostLogoutRedirectUri": "myapp://callback"
}
```

- `Authority`, `ClientId`, `Scope` and `RedirectUri` come from the identity provider's client registration. Do not invent them; ask, or leave placeholders and report sign-in as unconfigured.
- **Include `offline_access`** in `Scope`. Without a refresh token, `RefreshAsync` has nothing to renew: the stored tokens are cleared on the next start and the user signs in every launch.
- `openid` is required. `profile` and `email` add claims to the ID token; `api`-style scopes grant API access.
- A `ClientSecret` key exists, but a secret shipped inside an app is not secret. Prefer a public client with PKCE (OidcClient's default).

The same values in code, which win over the section:

```csharp
auth.AddOidc(oidc => oidc
    .Authority("https://demo.duendesoftware.com/")
    .ClientId("interactive.public")
    .Scope("openid profile email api offline_access")
    .RedirectUri("myapp://callback")
    .PostLogoutRedirectUri("myapp://callback"))
```

Advanced `OidcClientOptions` (PAR, token validation policy, discovery):

```csharp
auth.AddOidc(oidc => oidc.ConfigureOidcClientOptions(options =>
{
    options.Policy.Discovery.ValidateIssuerName = false;
}))
```

## Redirect URI per platform

- **WebAssembly**: the redirect and post-logout URIs are taken from `WebAuthenticationBroker.GetCurrentApplicationCallbackUri()` automatically, overriding the configured values. Register that URL with the identity provider.
- **Other platforms**: the configured `RedirectUri` is used. Opt into the broker's callback URI with `.AutoRedirectUriFromWebAuthenticationBroker()` (older docs call it `AutoRedirectUriFromAuthenticationBroker`, which does not exist).
- **Android**: the template adds `Platforms/Android/WebAuthenticationBrokerActivity.Android.cs` with the placeholder scheme `myprotocol`. Set `DataScheme` to the scheme of `RedirectUri` (`myapp` above), or the redirect never returns to the app.
- **iOS and macOS**: the redirect URI must use a custom scheme registered under `CFBundleURLTypes` in `Info.plist`. The template's top-level `CFBundleURLSchemes` key is ignored by iOS; move its scheme into `CFBundleURLTypes`.
- **WebAssembly**: the callback must be on the app's own origin (`<origin>/authentication-callback`); a custom scheme cannot work there.
- **Windows (WinAppSDK)**: `AddOidc` handles the redirect activation of the out-of-process browser itself.

## Behavior

- `LoginAsync(Dispatcher)` opens the browser; credentials are ignored. A cancelled or failed sign-in returns `false` and logs the provider's error.
- Stored tokens: `AccessToken`, `RefreshToken` and `IdToken` in `ITokenCache`. The HTTP handler sends the access token (`references/http.md`).
- `LogoutAsync` calls the provider's end-session endpoint and clears the tokens, which raises `LoggedOut`.
- To replace the browser (an embedded web view, a custom tab), register your own `Duende.IdentityModel.OidcClient.Browser.IBrowser` with `services.AddTransient<IBrowser, MyBrowser>()` in `ConfigureServices`.

The platform walkthrough is the docs' OpenID Connect tutorial: `uno_platform_docs_search("Uno OpenID Connect authentication tutorial")`.
