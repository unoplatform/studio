---
type: llm
---
The reply fixes MSAL (Uno.Extensions `AddMsal`) sign-in in an Uno Platform app that works on Android but fails on iOS and WebAssembly with a redirect URI mismatch.
Facts the judge needs: the MSAL provider applies each platform's redirect URI itself: `msal{ClientId}://auth` on Android, `msauth.{BundleId}://auth` on iOS, the `/authentication-callback` page on WebAssembly, `http://localhost` on desktop. A non-empty `RedirectUri` in the configuration section replaces those defaults on every head, and every Debug build loads `appsettings.development.json`. A value set only under `OperatingSystem.IsAndroid()` applies only there.
PASS if the Android redirect URI no longer applies to every head: the `RedirectUri` key is removed (or left empty) so each platform gets its default, or the value is set for Android only.
FAIL if one `RedirectUri` value still applies to every platform.
