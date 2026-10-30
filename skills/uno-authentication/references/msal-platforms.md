# MSAL: Redirect URIs and Platform Setup

What each head needs for MSAL sign-in to come back to the app. Registration and configuration are in `references/msal.md`. The complete working reference is the [MSAL sample in Uno.Samples](https://github.com/unoplatform/Uno.Samples/tree/master/UI/Authentication.MsalExtensionsDemo).

## Redirect URIs

The provider applies each platform's conventional redirect URI; do not add a per-platform `#if` for it.

| Platform | Redirect URI applied | Register in the app registration as |
|---|---|---|
| Android | `msal{ClientId}://auth` | Mobile and desktop applications, **Custom redirect URI**. Not the portal's Android option: that makes a broker `msauth://{package}/{hash}` URI |
| iOS | `msauth.{BundleId}://auth` | iOS/macOS: enter the bundle ID, the portal builds the URI |
| WebAssembly | the `WebAuthenticationBroker` callback, path `/authentication-callback` | **Single-page application (`spa`)** |
| Desktop (Skia: Windows, macOS, Linux) | `http://localhost` (system browser) | Mobile and desktop applications |
| WinAppSDK (`net10.0-windows10.*`) | none: the Windows broker (WAM) owns it | Mobile and desktop applications: `ms-appx-web://microsoft.aad.brokerplugin/{ClientId}` |

Precedence, lowest to highest: the platform default, `RedirectUri` in the configuration section, then anything `Builder(...)` sets. A `RedirectUri` set unconditionally applies to every head; guard a single-platform value with `OperatingSystem.IsAndroid()` and the like, or WebAssembly sign-in fails with a redirect-URI mismatch. `"UseDefaultPlatformRedirectUri": false` turns the defaults off.

The template sets `"RedirectUri": "myprotocol://auth"` in `appsettings.development.json`, which every Debug build loads (`UseEnvironment(Environments.Development)`), and an empty one in `appsettings.json`. Delete the key from both files: any non-empty value skips the table above on every head, so `msal<client-id>://auth` there fixes Android and breaks iOS, WebAssembly and desktop. The `KeychainSecurityGroup` key next to it binds to nothing; the iOS keychain group comes from `Entitlements.plist` below, or `Builder(app => app.WithIosKeychainSecurityGroup(...))`. The registrations are the developer's: list the URIs in the report, and do not claim sign-in works until they exist.

## Android

Without these, the browser sign-in succeeds but `LoginAsync` never completes. The template's `MsalActivity` uses the placeholder scheme `myprotocol`: replace it with `msal` followed by the client ID, and add the `auth` host.

```csharp
// Platforms/Android/MsalActivity.Android.cs
[Activity(Exported = true, LaunchMode = LaunchMode.SingleTop, NoHistory = true)]
[IntentFilter(
    [Intent.ActionView],
    Categories = [Intent.CategoryBrowsable, Intent.CategoryDefault],
    DataScheme = "msal<client-id>", // msal{ClientId}, a literal: keep in sync with appsettings
    DataHost = "auth")]
public class MsalActivity : BrowserTabActivity
{
}
```

`MainActivity.OnActivityResult` already forwards to `AuthenticationContinuationHelper.SetAuthenticationContinuationEventArgs` in the template; keep it.

## iOS

The template adds none of the three pieces iOS needs. Its `Info.plist` has a `myprotocol` entry under a top-level `CFBundleURLSchemes` key, which iOS ignores: replace it.

1. URL scheme in `Platforms/iOS/Info.plist`:

    ```xml
    <key>CFBundleURLTypes</key>
    <array>
        <dict>
            <key>CFBundleURLName</key>
            <string>com.microsoft.msal</string>
            <key>CFBundleURLSchemes</key>
            <array>
                <string>msauth.com.example.myapp</string>
            </array>
        </dict>
    </array>
    ```

2. An application delegate that hands the redirect to MSAL (with Skia rendering `App` is not the delegate):

    ```csharp
    // Platforms/iOS/MsalAppDelegate.iOS.cs
    public class MsalAppDelegate : Uno.UI.Runtime.Skia.AppleUIKit.UnoUIApplicationDelegate
    {
    #pragma warning disable CA1422 // OpenUrl is deprecated for UIScene apps; this app uses the delegate lifecycle
        public override bool OpenUrl(UIApplication application, NSUrl url, NSDictionary options)
            => AuthenticationContinuationHelper.SetAuthenticationContinuationEventArgs(url)
                || base.OpenUrl(application, url, options);
    #pragma warning restore CA1422
    }
    ```

    registered in `Platforms/iOS/Main.iOS.cs`: `.UseAppleUIKit(b => b.UseUIApplicationDelegate<MsalAppDelegate>())` on the `UnoPlatformHostBuilder`.

3. Keychain groups in `Platforms/iOS/Entitlements.plist` (the template ships an empty `<dict/>`; without this the first token save fails with `missing_entitlements`):

    ```xml
    <key>keychain-access-groups</key>
    <array>
        <string>$(AppIdentifierPrefix)$(CFBundleIdentifier)</string>
        <string>$(AppIdentifierPrefix)com.microsoft.adalcache</string>
    </array>
    ```

    Keep the bundle's own group first. A distribution build needs Keychain Sharing enabled on the App ID.

An incremental build can skip edits to `Info.plist` and `Entitlements.plist` without a word: after changing either, delete `bin` and `obj` for the iOS target and rebuild.

## WebAssembly

- Register the redirect URI under the **`spa`** platform; any other type fails the token request with `AADSTS90023` (CORS).
- Sign-in runs in a popup whose URL Uno polls: the popup must not be blocked, the origin must not send `Cross-Origin-Opener-Policy`, and the host must rewrite `/authentication-callback` to `index.html` (the template's `staticwebapp.config.json` does).
- `spa` refresh tokens last 24 hours, non-sliding: users sign in again at least daily.
- The token cache is cleartext browser storage, `localStorage` by default. For credentials prefer `SessionStorage` (cleared with the tab): `"KeyValueStorageConfiguration": { "BrowserCacheLocation": "SessionStorage" }`. `MemoryStorage` keeps nothing across reloads.
