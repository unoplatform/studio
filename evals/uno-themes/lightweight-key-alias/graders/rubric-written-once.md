---
type: llm
---
The reply overrides Uno Themes lightweight styling keys of a Material app's buttons (`FilledButtonBackground`, `OutlinedButtonBorderBrush`, ...) so they use brushes the theme already has (`TertiaryBrush`, `PrimaryBrush`).
Facts the judge needs: an alias such as `<StaticResource x:Key="FilledButtonBackground" ResourceKey="TertiaryBrush" />` placed once in App.xaml resources, outside `ThemeDictionaries`, resolves the target in the active theme, so it follows Light/Dark and palette changes. Aliases that also set the hover, pressed or focused variants of the same keys are fine.
PASS if each overridden key is defined once, outside `ThemeDictionaries`, as an alias to an existing theme brush.
FAIL if a key is defined again, identically, in both a `Light` and a `Default` theme dictionary, or is given a new `SolidColorBrush` or a hex color.
