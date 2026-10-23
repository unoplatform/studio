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

The same values in code. Any code option makes the provider ignore the configuration section entirely (`Settings.Options ?? Configuration.Get(name)`), so set every value here or none; a per-platform override of one value goes through configuration (`references/oidc-platforms.md`):

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

See `references/oidc-platforms.md`: a custom scheme on Android and iOS, the broker's callback on WebAssembly, and on the Skia desktop head an `IBrowser` of your own, because Uno's `WebAuthenticationBroker` is not implemented there and `LoginAsync` fails out of the box.

## Google

Google is an OIDC provider with two traps, checked against its discovery document and OidcClient 6.0.1:

- Its discovery document has no `end_session_endpoint`, so `LogoutAsync` throws `InvalidOperationException` ("Discovery document has no end session endpoint") after the tokens are cleared and `LoggedOut` has fired. Catch it in the logout command; the user is signed out of the app either way.
- A custom-scheme redirect must be the reverse DNS of a domain you control or of the client ID (`com.example.app:/oauth2redirect` or `com.googleusercontent.apps.<client-id>:/oauth2redirect`; the scheme needs a period, the path a single slash); Google no longer accepts custom schemes on Android, and gives desktop clients a loopback `http://127.0.0.1:<port>` URI. Check Google's native-app guide for the client type of each head before promising sign-in there, and do not reuse one `RedirectUri` across platforms.

## Behavior

- `LoginAsync(Dispatcher)` opens the browser; credentials are ignored. A cancelled or failed sign-in returns `false` and logs the provider's error.
- Stored tokens: `AccessToken`, `RefreshToken` and `IdToken` in `ITokenCache`. The HTTP handler sends the access token (`references/http.md`).
- `LogoutAsync` opens the end-session endpoint with only `post_logout_redirect_uri` (no `id_token_hint`, no `state`), waits for the browser to reach the post-logout URI or for OidcClient's 300 s browser timeout, then clears the tokens and raises `LoggedOut` whatever the browser returned: there is no cancel path that keeps the tokens. Many identity providers (Duende included) do not redirect back without `id_token_hint`, so the user sees the provider's logged-out page and the call waits for the timeout; a custom `IBrowser` can add the hint (`references/oidc-platforms.md`).
- To replace the browser (an embedded web view, a custom tab), register your own `Duende.IdentityModel.OidcClient.Browser.IBrowser` with `services.AddTransient<IBrowser, MyBrowser>()` in `ConfigureServices`.

Docs: `uno_platform_docs_fetch(sourcePath="external/uno.extensions/doc/Learn/Authentication/HowTo-OidcAuthentication.md")` and the walkthrough `external/uno.extensions/doc/Learn/Walkthrough/Authentication.Oidc.howto.md`. A search for "OpenID Connect tutorial" returns the older `guides/open-id-connect.md`, which drives `IdentityModel.OidcClient` by hand without Uno.Extensions.
