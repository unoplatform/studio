---
type: llm
---
The reply answers a developer on Uno.Sdk 7.0.22 who wants a map.
Facts the judge needs: Mapsui (`Mapsui.Uno.WinUI` 5.1.0 and 5.2.0) depends on the Uno 6 build of `SkiaSharp.Views.Uno.WinUI`, so on Uno.Sdk 7.x the build stops with `UNOB0023`. It has no 7.0 build yet. Uno's own `MapControl` (`Uno.WinUI.Maps`) covers only Android and iOS. The developer's options are pinning the app to Uno.Sdk 6.7 or waiting for a Mapsui build for Uno 7.
PASS if it tells the developer that Mapsui does not build on Uno.Sdk 7 yet.
FAIL if it gives Mapsui setup as if it works on 7.0.22, or hides the problem behind a suppressed check or forced dependency versions.