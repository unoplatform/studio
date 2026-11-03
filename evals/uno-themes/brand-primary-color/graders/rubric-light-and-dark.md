---
type: llm
---
The reply explains how to rebrand an Uno Platform app using Uno Material to a brand primary color, with a specific brand red for errors.
Facts the judge needs: the 280 semantic brushes and every control style derive from a palette of `*Color` keys (`PrimaryColor`, `OnPrimaryColor`, `PrimaryContainerColor`, ...); the palette is overridden on the theme through `<MaterialTheme.Colors><ut:ThemeColors xmlns:ut="using:Uno.Themes" ... /></MaterialTheme.Colors>`, either with a seed (`PrimarySeed="#0F62FE"`, which generates every role for Light and Dark) or with `OverrideSource`/`OverrideDictionary` pointing at a ResourceDictionary of `*Color` keys. The older `ColorOverrideSource`/`ColorOverrideDictionary` properties on `MaterialTheme` are obsolete but still work. A seed-only answer uses no override dictionary, so it needs no per-theme values and passes this criterion.
PASS if every override dictionary the reply uses supplies values for both the `Light` and the `Dark` (or `Default`) theme dictionaries, or the reply explains that both are needed.
FAIL if it gives a palette override with Light values only.
