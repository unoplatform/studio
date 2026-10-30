---
type: llm
---
The reply makes one Uno Platform page show white status bar icons on a navy status bar on iOS and Android.
Background (Uno Toolkit docs, StatusBar Extensions): `utu:StatusBar.Foreground` and `utu:StatusBar.Background` are attached properties set on the `Page` (`xmlns:utu="using:Uno.Toolkit.UI"`). `Foreground` takes a `StatusBarForegroundTheme` value (`None`, `Light` = white icons, `Dark` = black icons, `Auto`, `AutoInverse`), not a brush; `Background` takes a color or brush. They only work on iOS and Android, and on iOS `UIViewControllerBasedStatusBarAppearance` must be set to `false` in `Info.plist`.
PASS if all hold:
- It sets `utu:StatusBar.Foreground="Light"` and `utu:StatusBar.Background` to navy (a color or brush) on the profile `Page`.
- `Foreground` is given as the enum value, not a brush or color.
- It tells the user to set `UIViewControllerBasedStatusBarAppearance` to `false` in the iOS `Info.plist`.
FAIL if it sets `StatusBar.Foreground` to a brush/color, relies only on per-platform native code (Android `Window.SetStatusBarColor`, iOS `UIApplication.StatusBarStyle`) instead of the Toolkit attached properties, or omits the iOS `Info.plist` setting.
