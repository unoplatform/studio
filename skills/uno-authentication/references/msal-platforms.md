# MSAL: Redirect URIs and Platform Setup

What each head needs for MSAL sign-in to come back to the app. Registration and configuration are in `references/msal.md`. The complete working reference is the [MSAL sample in Uno.Samples](https://github.com/unoplatform/Uno.Samples/tree/master/UI/Authentication.MsalExtensionsDemo).

## Redirect URIs

The provider applies each platform's conventional redirect URI; do not add a per-platform `#if` for it.

| Platform | Redirect URI applied | Register in the app registration as |
|---|---|---|
| Android | `msal{ClientId}://auth` | Android / mobile |
| iOS | `msauth.{BundleId}://auth` | iOS / mobile |
| WebAssembly | the `WebAuthenticationBroker` callback, path `/authentication-callback` | **Single-page application (`spa`)** |
| Desktop (Skia: Windows, macOS, Linux) | `http://localhost` (system browser) | Mobile and desktop applications |
| WinAppSDK (`net10.0-windows10.*`) | none: the Windows broker (WAM) owns it | Mobile and desktop applications (broker URI) |

Precedence, lowest to highest: the platform default, `RedirectUri` in the configuration section, then anything `Builder(...)` sets. A `RedirectUri` set unconditionally applies to every head; guard a single-platform value with `OperatingSystem.IsAndroid()` and the like, or WebAssembly sign-in fails with a redirect-URI mismatch. `"UseDefaultPlatformRedirectUri": false` turns the defaults off.

The registrations themselves are the developer's: list the URIs they must register in the report, do not claim sign-in works until they have.

## Android

Without these, the browser sign-in succeeds but `LoginAsync` never completes. The template's `MsalActivity` uses the placeholder scheme `myprotocol`: replace it with `msal` followed by the client ID, and add the `auth` host.

```csharp
// Platforms/Android/MsalActivity.Android.cs
[Activity(Exported = true, LaunchMode = LaunchMode.SingleTask, NoHistory = true)]
[IntentFilter(
    [Intent.ActionView],
    Categories = [Intent.CategoryBrowsable, Intent.CategoryDefault],
    DataScheme = "msal161a9fb5-3b16-487a-81a2-ac45dcc0ad3b", // msal{ClientId}, a literal: keep in sync with appsettings
    DataHost = "auth")]
public class MsalActivity : BrowserTabActivity
{
}
```

`MainActivity` forwards activity results (the template already has this):

```csharp
protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
{
    base.OnActivityResult(requestCode, resultCode, data);
    AuthenticationContinuationHelper.SetAuthenticationContinuationEventArgs(requestCode, resultCode, data);
}
```

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

## WebAssembly

- Register the redirect URI under the **`spa`** platform; a public-client registration fails the token request (CORS) in the browser.
- `spa` refresh tokens last 24 hours, non-sliding: users sign in again at least daily.
- The token cache is cleartext browser storage, `localStorage` by default. For credentials prefer `SessionStorage` (cleared with the tab): `"KeyValueStorageConfiguration": { "BrowserCacheLocation": "SessionStorage" }`. `MemoryStorage` keeps nothing across reloads.

## Mac Catalyst

Not supported: `AddMsal` throws `PlatformNotSupportedException` at host build. Guard the registration with `!OperatingSystem.IsMacCatalyst()`.
