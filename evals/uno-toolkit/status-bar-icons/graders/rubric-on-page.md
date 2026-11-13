---
type: llm
---
The reply makes one Uno Platform page show white status bar icons on a navy status bar on iOS and Android.
Facts the judge needs: `utu:StatusBar.Foreground` and `utu:StatusBar.Background` are attached properties set on the `Page` (`xmlns:utu="using:Uno.Toolkit.UI"`). `Foreground` takes a `StatusBarForegroundTheme` value (`Light` = white icons); `Background` takes a color or brush. They only work on iOS and Android.
PASS if it sets `utu:StatusBar.Foreground` and a navy `utu:StatusBar.Background` on the profile `Page`.
FAIL if it relies only on per-platform native code (Android `Window.SetStatusBarColor`, iOS `UIApplication.StatusBarStyle`) instead of the Toolkit attached properties, or sets them app-wide instead of on that page.
