---
type: llm
---
The reply explains how to make an Uno Platform app (recommended template, Simple theme with Uno Toolkit) denser app-wide.
Facts the judge needs: the recommended preset puts `<utus:SimpleToolkitTheme xmlns:utus="using:Uno.Toolkit.UI.Simple" />` in App.xaml; it derives from `SimpleTheme` and exposes `DefaultDensity` (`Compact` / `Regular` / `Comfy`), which scales the `Space*` design tokens that control padding and margins (×0.75 for Compact); control heights (`ControlHeight*`) and icon sizes stay fixed. `DefaultSpacing` (base unit, default 8) can also be lowered.
PASS if it keeps the single `SimpleToolkitTheme`.
FAIL if it adds a second, bare `SimpleTheme` next to `SimpleToolkitTheme`.
