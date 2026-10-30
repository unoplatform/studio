---
type: llm
---
The reply explains how to add an Uno Toolkit TabBar to an existing Uno Platform app created with `-preset blank`.
Facts the judge needs: the blank preset uses Fluent styles and has no Material or Simple theme, so theme keys such as `BottomTabBarStyle`, `BottomTabBarItemStyle`, `MaterialBottomTabBarStyle` and semantic brushes such as `BackgroundBrush` do not resolve unless a theme is added (`Material` or `SimpleTheme` in `UnoFeatures` plus the theme resources in App.xaml).
PASS if the XAML does not use those Material/Simple-only keys, or it adds a theme that provides them, or it warns that they need such a theme.
FAIL if it uses Material/Simple-only style keys or brushes on the blank (Fluent) app without adding or mentioning the theme.
