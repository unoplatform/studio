# OIDC: Redirect URIs and Platform Setup

How the identity provider's redirect gets back into the app on each head. Registration and configuration are in `references/oidc.md`. Verified on Uno.Sdk 7.0.0-dev.77 with the Duende demo server; the working reference is `UI/Authentication.OidcExtensionsDemo` in Uno.Samples.

The provider sends the configured `RedirectUri` everywhere except WebAssembly. What differs per head is the browser that opens the page and the route back, through Uno's `WebAuthenticationBroker` or, on desktop, a browser you supply.

| Platform | Redirect URI | Route back |
|---|---|---|
| Android | custom scheme, `myapp://callback` | intent filter on an activity deriving from `WebAuthenticationBrokerActivityBase` |
| iOS | custom scheme | `ASWebAuthenticationSession`, scheme declared under `CFBundleURLTypes` |
| WebAssembly | `<origin>/authentication-callback`, set by the provider | popup polled by the broker |
| Desktop (Skia) | loopback `http://127.0.0.1:<port>/callback` | an `IBrowser` you register: the broker is not implemented there |
| WinAppSDK | custom scheme in `Package.appxmanifest` | the provider's out-of-process browser |

`AutoRedirectUriFromWebAuthenticationBroker()` replaces both URIs with `GetCurrentApplicationCallbackUri()`: `<origin>/authentication-callback` on WebAssembly, `<first scheme>:/authentication-callback` (one slash) on Android and iOS, from the first scheme the broker finds; it throws on desktop. Register exactly that string.

## Desktop (Skia)

`AddOidc` registers `WebAuthenticatorBrowser`, which calls `WebAuthenticationBroker.AuthenticateAsync`; on the Skia desktop head that throws `NotImplementedException`, the provider logs `Error logging in: System.NotImplementedException` and `LoginAsync` returns `false`. Register your own `IBrowser` (`Duende.IdentityModel.OidcClient.Browser`), as the docs' "advanced customizations" say: open the system browser on `options.StartUrl`, listen with `HttpListener` on `options.EndUrl` (the configured redirect URI, so make it a loopback address), answer the one request with a small page, and return `new BrowserResult { ResultType = Success, Response = context.Request.Url.OriginalString }`. Register it in the desktop head only, with `services.AddTransient<IBrowser, SystemBrowser>()` in a `ConfigureServices` call placed **after** `UseAuthentication` in the host chain: `AddOidc` registers its own `WebAuthenticatorBrowser` the same way, and the last `IBrowser` registration is the one the provider resolves.

Give that head its own redirect URIs through configuration, not code: add `AddInMemoryCollection` entries for `OidcAuthentication:RedirectUri` and `:PostLogoutRedirectUri` in a `ConfigureAppConfiguration` call after the embedded appsettings (`references/oidc.md` explains why code options cannot override one value). Keep the port fixed so the URI can be registered once.

Sign-out on desktop: the provider's end-session request has no `id_token_hint`, so the identity provider shows its logged-out page and does not redirect to the listener; `LogoutAsync` waits the whole browser timeout. In the `IBrowser`, append `id_token_hint=<IdToken from ITokenCache>` to an end-session URL that lacks it; the identity provider then offers (or performs) the redirect back.

## Android

The template's `Platforms/Android/WebAuthenticationBrokerActivity.Android.cs` has the placeholder scheme `myprotocol`. The broker discovers the activity by its base class and reads the scheme off its `[IntentFilter]`; set `DataScheme` to the scheme of `RedirectUri`, as a constant, or the browser has nowhere to deliver the callback and `LoginAsync` never completes.

```csharp
[Activity(NoHistory = true, LaunchMode = LaunchMode.SingleTop, Exported = true)]
[IntentFilter([Intent.ActionView],
    Categories = [Intent.CategoryDefault, Intent.CategoryBrowsable],
    DataScheme = "myapp")]
public class WebAuthenticationBrokerActivity : Uno.AuthenticationBroker.WebAuthenticationBrokerActivityBase
{
}
```

## iOS

The broker reads the schemes under `CFBundleURLTypes` > `CFBundleURLSchemes` in `Info.plist` and refuses to start a sign-in whose callback scheme is not among them ("No schemes defined in info.plist"). The template writes a top-level `CFBundleURLSchemes` key, which iOS and the broker both ignore: replace it with

```xml
<key>CFBundleURLTypes</key>
<array>
    <dict>
        <key>CFBundleURLName</key>
        <string>com.example.myapp.oidc</string>
        <key>CFBundleURLSchemes</key>
        <array>
            <string>myapp</string>
        </array>
    </dict>
</array>
```

No app-delegate or scene-delegate code is involved: `ASWebAuthenticationSession` returns the callback itself. The token store is the keychain, so `Platforms/iOS/Entitlements.plist` needs `keychain-access-groups` with `$(AppIdentifierPrefix)$(CFBundleIdentifier)` (the template ships an empty `<dict/>`). An incremental build can skip `Info.plist` and `Entitlements.plist` edits: delete `bin` and `obj` for the iOS target after changing them.

## WebAssembly

Nothing to configure: the provider always uses `<origin>/authentication-callback`. Register that URL for every origin you serve from. Sign-in runs in a popup whose URL the broker polls: the popup must not be blocked, the origin must not send `Cross-Origin-Opener-Policy`, and the host must serve `index.html` for the callback path (the template's `staticwebapp.config.json` rewrites unknown paths). Tokens live in browser storage; `KeyValueStorageConfiguration:BrowserCacheLocation` picks `LocalStorage` (default), `SessionStorage` or `MemoryStorage`.
