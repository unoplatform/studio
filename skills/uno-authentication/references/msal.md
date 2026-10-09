# MSAL (Microsoft Entra ID, Microsoft accounts, B2C)

`AddMsal` signs users in with the Microsoft identity platform through MSAL.NET. The user signs in on Microsoft's page, so the login page has a single "Sign in" button and no credential fields. Platform redirect and storage setup (Android, iOS, WebAssembly) is in `references/msal-platforms.md`; read it before targeting a mobile or browser head.

Feature: `AuthenticationMsal` in `<UnoFeatures>` (it replaces `Authentication`). New app: `dotnet new unoapp -preset recommended -auth msal`.

## Registration

`AddMsal` needs the app's `Window`, so use the `Configure` overload that provides it. The overload without a window is obsolete; without a window, sign-in can fail with an `MsalClientException` saying only a loopback redirect URI is supported.

```csharp
var builder = this.CreateBuilder(args)
    .Configure((host, window) => host
        // ...
        .UseAuthentication(auth =>
        {
            if (!OperatingSystem.IsMacCatalyst()) // MSAL has no Mac Catalyst build: AddMsal throws there
            {
                auth.AddMsal(window, name: "MsalAuthentication");
            }
        }));
```

Drop the Mac Catalyst guard when the app has no Catalyst target. The `name` is also the configuration section; the template uses `MsalAuthentication`, the default is `Msal`.

```json
"MsalAuthentication": {
  "ClientId": "161a9fb5-3b16-487a-81a2-ac45dcc0ad3b",
  "Scopes": [ "User.Read" ]
}
```

- `ClientId` (a GUID from the app registration) is required; without it the app throws `MsalClientException: No ClientId was specified`. Never invent one: ask the developer for it, or leave the placeholder and report sign-in as unconfigured.
- `Scopes`: request at least one API scope. `LoginAsync` returns `false` when no access token comes back (B2C with only `openid`/`offline_access`, for example).
- Other keys the section binds: MSAL's `PublicClientApplicationOptions` (`TenantId`, `Instance`, `RedirectUri`, ...), plus `B2CAuthority`, `UseDefaultPlatformRedirectUri`, `InteractiveTimeout`, `KeychainServiceName`, `KeychainAccountName`, `AllowUnprotectedTokenCacheFallback`.

The same settings in code, where `Builder` configures MSAL's `PublicClientApplicationBuilder` and runs after the configuration section, so it wins:

```csharp
auth.AddMsal(window, msal => msal
    .Builder(app => app.WithClientId("161a9fb5-3b16-487a-81a2-ac45dcc0ad3b"))
    .Scopes(["User.Read"]));
```

Other builder calls:

| Call | Use |
|---|---|
| `.InteractiveBuilder(i => i.WithPrompt(Prompt.SelectAccount).WithLoginHint(...))` | Per-sign-in options (`WithPrompt`, `WithLoginHint`, `WithExtraScopeToConsent`, `WithSystemWebViewOptions`), unreachable from `Builder` |
| `.Storage(s => s.WithMacKeyChain(service, account))` | Desktop token cache storage properties |

## Signing in and out

- **`LoginAsync` must receive an `IDispatcher`**: `Authentication.LoginAsync(Dispatcher)`. The convenience overload without one makes the MSAL provider throw `ArgumentNullException`, even when a cached account would have signed in silently. Inject `IDispatcher` into the login Model.
- `LogoutAsync(ct)` needs no dispatcher. It removes MSAL's accounts and cache; the identity provider's browser session survives, so the next sign-in may complete without a prompt.
- `RefreshAsync` at startup acquires a token silently from MSAL's cache; the flow in `references/flow.md` applies unchanged.
- On desktop (Skia), an interactive sign-in the user abandons times out after 5 minutes (the system browser cannot report a closed window). Change it with `"InteractiveTimeout": "00:02:00"`.

## Token cache

MSAL's own cache (refresh and ID tokens) persists per platform: DPAPI-encrypted file on Windows, Keychain on macOS, libsecret on Linux, MSAL's native store on Android and iOS, browser storage on WebAssembly. The access token used by the HTTP handler and a copy of the ID token are in `ITokenCache`, under `TokenCacheExtensions.AccessTokenKey` and `IdTokenKey`.

On a Linux session without a keyring the cache stays in memory (the user signs in again after a restart) unless `"AllowUnprotectedTokenCacheFallback": true` opts into a plaintext file.

## User claims

The ID token's payload holds the user's claims (`name`, `preferred_username`, `emails`, B2C attributes such as `extension_Role`). Read it with `await tokenCache.TokenAsync(TokenCacheExtensions.IdTokenKey, ct)`, split on `.`, Base64Url-decode the second segment (`System.Buffers.Text.Base64Url.DecodeFromChars`) and deserialize with a source-generated `JsonSerializerContext`. Decode once after sign-in and clear it on `ITokenCache.Cleared`. Use claims to shape the UI; the API enforces authorization from the access token.

## Azure AD B2C

Set `"B2CAuthority": "https://contoso.b2clogin.com/tfp/contoso.onmicrosoft.com/B2C_1_signupsignin"` in the section and request an API scope (or the app's own client ID) so an access token is issued. On WinAppSDK, B2C falls back to the system browser: set `"RedirectUri": "http://localhost"` and register it under "Mobile and desktop applications". One user flow per provider.

Version note: written against Uno.Extensions 7.4 (Uno.Sdk 6.8), which requires Uno 6.8 or later. Interactive sign-in on Android, iOS and WebAssembly with `SkiaRenderer` silently does nothing on Uno 6.7.x.
