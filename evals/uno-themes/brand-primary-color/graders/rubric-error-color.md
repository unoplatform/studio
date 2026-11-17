---
type: llm
---
The reply explains how to rebrand an Uno Platform app using Uno Material to a brand primary color, with a specific brand red for errors.
Facts the judge needs: the 280 semantic brushes and every control style derive from a palette of `*Color` keys (`PrimaryColor`, `OnPrimaryColor`, `PrimaryContainerColor`, ...); the palette is overridden on the theme through `<MaterialTheme.Colors><ut:ThemeColors xmlns:ut="using:Uno.Themes" ... /></MaterialTheme.Colors>`, either with a seed (`PrimarySeed="#0F62FE"`, which generates every role for Light and Dark) or with `OverrideSource`/`OverrideDictionary` pointing at a ResourceDictionary of `*Color` keys. The older `ColorOverrideSource`/`ColorOverrideDictionary` properties on `MaterialTheme` are obsolete but still work. A seed derives the other roles algorithmically, so a specific brand error red is set by overriding `ErrorColor` (and related error roles) in the override dictionary, which can be combined with `PrimarySeed` on the same `ThemeColors` and wins over seed-generated values.
PASS if the brand red is applied by overriding `ErrorColor` (and, ideally, the related error roles) in the palette.
FAIL if it applies the red by restyling controls, or leaves the seed-generated error color.
