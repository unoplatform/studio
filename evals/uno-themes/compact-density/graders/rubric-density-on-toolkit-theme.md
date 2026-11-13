---
type: llm
---
The reply explains how to make an Uno Platform app (recommended template, Simple theme with Uno Toolkit) denser app-wide.
Facts the judge needs: the recommended preset puts `<utus:SimpleToolkitTheme xmlns:utus="using:Uno.Toolkit.UI.Simple" />` in App.xaml; it derives from `SimpleTheme` and exposes `DefaultDensity` (`Compact` / `Regular` / `Comfy`), which scales the `Space*` design tokens that control padding and margins (×0.75 for Compact); control heights (`ControlHeight*`) and icon sizes stay fixed. `DefaultSpacing` (base unit, default 8) can also be lowered.
PASS if it sets `DefaultDensity="Compact"` (optionally with a smaller `DefaultSpacing`) on the existing `SimpleToolkitTheme` element in App.xaml.
FAIL if the main approach is editing margins and paddings per page, global implicit styles with literal paddings, WinUI's `ContentControlThemeFontSize` or Fluent compact resource dictionary alone, or a property that does not exist (for example `DefaultSize`).
