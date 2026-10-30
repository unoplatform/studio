# OIDC: Redirect URIs and Platform Setup

How the identity provider's redirect gets back into the app on each head. Registration and configuration are in `references/oidc.md`. Verified with the Duende demo server on Android with Uno.Sdk 7.0.0-dev.77 (Uno.Extensions 8.0.0-dev.85), and on desktop, WebAssembly and an iOS 18.5 simulator with Uno.Sdk 7.0.0-dev.89 (Uno.Extensions 8.0.0-dev.135); the working reference is `UI/Authentication.OidcExtensionsDemo` in Uno.Samples.

The provider sends the configured `RedirectUri` everywhere except WebAssembly. What differs per head is the browser that opens the page and the route back, through Uno's `WebAuthenticationBroker` or, on desktop, a browser you supply.

| Platform | Redirect URI | Route back |
|---|---|---|
| Android | custom scheme, `myapp://callback` | intent filter on an activity deriving from `WebAuthenticationBrokerActivityBase` |
| iOS | custom scheme | `ASWebAuthenticationSession`, scheme declared under `CFBundleURLTypes` |
| WebAssembly | `<origin>/authentication-callback`, set by the provider | popup polled by the broker |
| Desktop (Skia) | loopback `http://localhost:<port>/authentication-callback` | the loopback broker `AddOidc` registers since Uno.Extensions 8.0.0-dev.135; before that, a browser you supply |
| WinAppSDK | custom scheme in `Package.appxmanifest` | the provider's out-of-process browser |

`AutoRedirectUriFromWebAuthenticationBroker()` replaces both URIs with `GetCurrentApplicationCallbackUri()`: `<origin>/authentication-callback` on WebAssembly, `<first scheme>:/authentication-callback` (one slash) on Android and iOS, from the first scheme the broker finds, and on desktop since dev.135 `http://localhost:{port}/authentication-callback` with a free port probed once per process unless `WinRTFeatureConfiguration.WebAuthenticationBroker.DefaultReturnUri` pins it (before dev.135 it throws there). Register exactly that string, which on desktop means pinning the port.

## Desktop (Skia)

Version-gated: since Uno.Extensions 8.0.0-dev.135 `AddOidc` registers a loopback `WebAuthenticationBroker` and the head only needs a `http://localhost:<port>/authentication-callback` redirect URI, set through configuration; on older versions (Uno.Sdk before 7.0.0-dev.95) sign-in fails with `NotImplementedException` until the app opts in or brings its own browser. Read `references/oidc-desktop.md` for the version check, the opt-in, the ordering and logging rules, and the pre-dev.135 workaround.

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
