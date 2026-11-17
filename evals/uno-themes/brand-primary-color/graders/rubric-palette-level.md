---
type: llm
---
The reply explains how to rebrand an Uno Platform app using Uno Material to a brand primary color, with a specific brand red for errors.
Facts the judge needs: the 280 semantic brushes and every control style derive from a palette of `*Color` keys (`PrimaryColor`, `OnPrimaryColor`, `PrimaryContainerColor`, ...); the palette is overridden on the theme through `<MaterialTheme.Colors><ut:ThemeColors xmlns:ut="using:Uno.Themes" ... /></MaterialTheme.Colors>`, either with a seed (`PrimarySeed="#0F62FE"`, which generates every role for Light and Dark) or with `OverrideSource`/`OverrideDictionary` pointing at a ResourceDictionary of `*Color` keys. The older `ColorOverrideSource`/`ColorOverrideDictionary` properties on `MaterialTheme` are obsolete but still work.
PASS if the change is made once at the palette level on the theme element in App.xaml (a seed, or an override dictionary of `*Color` keys such as `PrimaryColor`), so all controls follow.
FAIL if it restyles controls one by one, copies control templates, overrides only per-control keys like `FilledButtonBackground` as the main approach, or sets only a WinUI `SystemAccentColor`.
