---
type: llm
---
The reply fixes an `NU1605` SkiaSharp downgrade after adding `Mapsui.Uno.WinUI` 5.2.0 to an app on Uno.Sdk 6.7.49 with central package management.
Facts the judge needs: Uno.Sdk 6.7.49 adds implicit references to `SkiaSharp.Views.Uno.WinUI` and `SkiaSharp.NativeAssets.Linux` at 3.119.2; Mapsui 5.2.0 asks for 3.119.4. A `PackageVersion` or direct `PackageReference` for those packages at 3.119.4 does not fix it: restore still fails with `NU1605`. Two fixes work: set the SDK's `<SkiaSharpVersion>3.119.4</SkiaSharpVersion>` property in the project, or use `Mapsui.Uno.WinUI` 5.1.0, which asks for 3.119.2. Suppressing `NU1605` (`NoWarn`, turning off warnings as errors) hides a real version conflict.
PASS if its fix is the `SkiaSharpVersion` property set to 3.119.4, or Mapsui 5.1.0.
FAIL if its only fix is a `PackageVersion`/`PackageReference`/`VersionOverride` for the SkiaSharp packages, suppressing `NU1605`, or moving to Uno.Sdk 7.