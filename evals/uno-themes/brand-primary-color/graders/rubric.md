---
type: llm
---
The reply explains how to rebrand an Uno Platform app using Uno Material to a brand primary color.
Uno Themes facts the judge needs: the 280 semantic brushes and every control style derive from a palette of `*Color` keys (`PrimaryColor`, `OnPrimaryColor`, `PrimaryContainerColor`, ...); the palette is overridden on the theme through `<MaterialTheme.Colors><ut:ThemeColors xmlns:ut="using:Uno.Themes" ... /></MaterialTheme.Colors>`, either with a seed (`PrimarySeed="#0F62FE"`, which generates every role for Light and Dark) or with `OverrideSource`/`OverrideDictionary` pointing at a ResourceDictionary of `*Color` keys. The older `ColorOverrideSource`/`ColorOverrideDictionary` properties on `MaterialTheme` are obsolete but still work. Material Theme Builder can generate the palette file. A seed derives the other roles algorithmically, so a specific brand error red is set by overriding `ErrorColor` (and related error roles) in the override dictionary; `OverrideSource`/`OverrideDictionary` can be combined with `PrimarySeed` on the same `ThemeColors` and wins over seed-generated values.
PASS if all hold:
- The change is made once at the palette level on the theme element in App.xaml (a seed, or an override dictionary of `*Color` keys such as `PrimaryColor`), so all controls follow.
- The brand red is applied by overriding `ErrorColor` (and, ideally, the related error roles) in the palette, not by restyling controls.
- If it uses an override dictionary, it supplies values for both the `Light` and the `Dark` (or `Default`) theme dictionaries (or explains that both are needed); a seed answer needs no per-theme values.
FAIL if it restyles controls one by one, copies control templates, overrides only per-control keys like `FilledButtonBackground` as the main approach, sets only a WinUI `SystemAccentColor`, or gives a palette override with Light values only.
