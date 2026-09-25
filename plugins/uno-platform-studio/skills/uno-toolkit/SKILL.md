---
name: uno-toolkit
description: "Uno Toolkit (Uno.Toolkit.UI, xmlns:utu) controls and helpers for Uno Platform apps: TabBar, NavigationBar, CardContentControl, Chip/ChipGroup, DrawerControl, SafeArea, AutoLayout, FlexPanel, LoadingView, ShadowContainer, Divider, ExtendedSplashScreen, ZoomContentControl, segmented controls, ResponsiveView/ResponsiveExtension, attached-property extensions (Command, Input, ItemsRepeater selection, Selector/PipsPager, FlipView, Progress, StatusBar, TabBarItem, VisualStateManager, AncestorBinding, ResourceExtensions), lightweight styling, Material/Cupertino toolkit themes, and C# Markup. Use whenever an Uno app needs a card or tile, tabs or bottom bar, app bar with back button, side drawer or bottom sheet, chips/tags/filters, loading overlay, shadow, notch/safe-area/keyboard handling, responsive layout, flexbox or Figma-style spacing, splash screen, zoom/pan, form focus flow, or carousel, even when the user asks for a Border, Grid, or StackPanel that one of these controls should replace."
metadata:
  author: uno-platform
  version: "3.0"
  category: toolkit
---

# Uno Toolkit

Uno Toolkit is the control and helper library that ships with Uno Platform projects (`<UnoFeatures>Toolkit</UnoFeatures>`, `xmlns:utu="using:Uno.Toolkit.UI"`). Generic agents reach for raw `Border`, `Grid`, and code-behind where a Toolkit control already exists, and they invent member names on the controls they do know. This skill routes you to the one-page reference for each control and lists the rules that catch those mistakes.

## Workflow

1. Find the need in the topic map and read the matching `references/*.md` before writing XAML. Each reference is short and names the exact properties; do not guess member names from CSS, WPF, or MAUI memory.
2. Ground details in the official docs: call `uno_platform_docs_search(...)`, then `uno_platform_docs_fetch(sourcePath="…")` with the `sourcePath` from a result (a relative `.md` path). Never pass a URL, `.html` link, or hand-built path.
3. Check the generated XAML against the critical rules below; most are compile-clean but render wrong or empty at runtime.

## Topic map

### Controls

| Task / need | Read | Key API |
|-------------|------|---------|
| Card, tile, elevated/filled/outlined container (any rounded `Border` with a background) | `references/card.md` | `CardContentControl`, `ElevatedCardContentControlStyle`, `Content="{Binding}"` |
| Bottom tabs, top tabs, vertical tabs with icons, labels, badges | `references/tabbar.md` | `TabBar`, `TabBarItem`, `BottomTabBarStyle` |
| 2 to 5 mutually exclusive inline options, view switcher | `references/segmented-controls.md` | `TabBar` + `SegmentedStyle` |
| Page app bar with title, back button, action buttons | `references/navigationbar.md` | `NavigationBar`, `MainCommand`, `PrimaryCommands` |
| Swipe-open side drawer, bottom sheet, gesture flyout | `references/drawer.md` | `DrawerControl`, `DrawerFlyoutPresenter`, `DrawerOpenDirection` |
| Chips, tags, filter pills, tag input, suggestions | `references/chip.md` | `Chip`, `ChipGroup`, `FilterChipStyle` |
| Content under a notch, status bar, home indicator, or the on-screen keyboard | `references/safearea.md` | `SafeArea`, `Insets="SoftInput"` |
| Figma auto-layout rows/columns with spacing and alignment | `references/autolayout.md` | `AutoLayout`, `Spacing`, `Justify`, `CounterAlignment` |
| CSS flexbox: grow/shrink/basis, wrapping, gaps | `references/flexpanel.md` | `FlexPanel`, `Grow`, `Basis`, `Wrap`, `ColumnGap` |
| Spinner or skeleton while a command, feed, or state is busy | `references/loadingview.md` | `LoadingView`, `ILoadable`, `CompositeLoadableSource` |
| Layered drop shadows or inset shadows | `references/shadowcontainer.md` | `ShadowContainer`, `Shadow`, `IsInner` |
| Thin separator line, optional subheader | `references/divider.md` | `Divider`, `SubHeader` |
| Loading screen that extends the native splash | `references/extendedsplashscreen.md` | `ExtendedSplashScreen`, `Init()` on Android |
| Zoom and pan images, maps, documents | `references/zoomcontentcontrol.md` | `ZoomContentControl`, `AutoFit`, `ZoomTo` |
| One layout adapting to phone, tablet, desktop | `references/responsive.md` | `{utu:Responsive ...}`, `ResponsiveView`, `ResponsiveLayout` |

### Attached-property extensions and setup

| Task / need | Read | Key API |
|-------------|------|---------|
| Install Toolkit, pick a theme, CLI scaffold | `references/getting-started.md` | `<UnoFeatures>Toolkit</UnoFeatures>`, `dotnet new unoapp -toolkit` |
| Material Design 3 look for Toolkit controls | `references/material-theme.md` | `MaterialToolkitTheme`, `ColorOverrideSource` |
| iOS look for Toolkit controls | `references/cupertino-theme.md` | `CupertinoToolkitTheme` |
| Fluent C# instead of XAML | `references/csharp-markup.md` | `Uno.Toolkit.WinUI.Markup` |
| Recolor or respace a control without rewriting its template | `references/lightweight-styling.md` | `{ControlName}{State}{Property}` resource keys |
| Per-control resource overrides, visual variants | `references/resource-extensions.md` | `utu:ResourceExtensions.Resources` |
| Fire a command from Enter, item click, toggle, or tap without code-behind | `references/command-extensions.md` | `utu:CommandExtensions.Command` |
| Form focus flow, keyboard dismiss, Next/Done return key | `references/input-extensions.md` | `utu:InputExtensions.AutoFocusNext`, `ReturnType` |
| Selection or fetch-on-scroll on `ItemsRepeater` | `references/itemsrepeater-extensions.md` | `utu:ItemsRepeaterExtensions.SelectedItem`, `SelectionMode` |
| Sync a `PipsPager` with a `FlipView` or `ListView` carousel | `references/selector-extensions.md` | `utu:SelectorExtensions.PipsPager` |
| Previous/Next arrows on `FlipView` for desktop | `references/flipview-extensions.md` | `FlipViewExtensions` |
| Toggle every `ProgressRing`/`ProgressBar` in a subtree | `references/progress-extensions.md` | `utu:ProgressExtensions.IsExecuting` |
| Status bar icon color and background on mobile | `references/statusbar-extensions.md` | `utu:StatusBar.Foreground`, `StatusBar.Background` |
| Navigate on `TabBarItem` click | `references/tabbaritem-extensions.md` | `TabBarItemExtensions` |
| Visual states driven by a view-model property | `references/visualstatemanager-extensions.md` | `utu:VisualStateManagerExtensions.States` |
| Bind from inside a `DataTemplate` to the page ViewModel | `references/ancestor-binding.md` | `{utu:AncestorBinding}`, `{utu:ItemsControlBinding}` |
| Read or switch dark/light mode from C# | `references/system-theme-helper.md` | `SystemThemeHelper.SetRootTheme` |

## Critical rules

- **A rounded `Border` with a background, padding, or border brush is a card. Write it as `CardContentControl`.** The XAML parser flags Border-as-card as `GRD1102`. `CardContentControl` gives correct elevation, corner radius, theming, and accessibility for free; `Border` stays right only for hairlines, plain fills without rounding, clipping with `CornerRadius` alone, and primitives inside a `ControlTemplate`.
- **Never use `Card`.** Its fixed slots (Header, SubHeader, Avatar) fight custom layouts. `CardContentControl` with a `ContentTemplate` is the only card control to use.
- **`CardContentControl` inside any `DataTemplate` needs `Content="{Binding}"`.** The inner template inherits its `DataContext` only from `Content`; without the binding every card renders empty. Do not use indexed bindings (`Items[0].Label`) inside card templates.
- **`CardContentControl` sizes to content.** Set `HorizontalAlignment="Stretch"` (and `VerticalAlignment="Stretch"` for pane-filling cards) on the control itself; sizing the inner content does nothing.
- **Any mobile page with a `TextBox` or `PasswordBox` needs `SafeArea` with `Insets="SoftInput"`** (or `SoftInput,Bottom`). The keyboard will cover the inputs otherwise. Use it as well for footers and bottom bars that must clear the home indicator, gesture bar, or notch.
- **`TabBar` always gets an explicit style** (`BottomTabBarStyle`, `TopTabBarStyle`, `VerticalTabBarStyle`, or `SegmentedStyle`), applied to the `TabBar`, not to items. Without it the control renders unstyled. `SegmentedTabBarStyle` does not exist. Wiring tabs to pages is navigation work: see the `uno-navigation` skill (`references/tabbar.md`) for `uen:Region.Name` on each `TabBarItem`.
- **`NavigationBar.MainCommand` is an `AppBarButton`**, as are `PrimaryCommands` and `SecondaryCommands` items. There is no `NavigationBarMainCommand` type. On iOS and Android the bar renders natively, so keep custom content to what the native bar supports.
- **Do not invent member names.** The references list the real surface. Known traps: `AutoLayout` children use `utu:AutoLayout.CounterAlignment` (not `CounterAxisAlignment`); `FlexPanel` has `ColumnGap`/`RowGap` (no `Gap`, no `Order`, no `Flex` shorthand) and `Basis` beats `Width`; `ZoomContentControl` has `AutoFit` (not `AutoFitToCanvas`) and `ZoomTo(float)` instead of a settable `Zoom`; `CommandExtensions` has no `CommandTrigger`; `StatusBar.Foreground` is an enum (`Light`, `Dark`, `Auto`, `AutoInverse`), not a brush; `SystemThemeHelper` has no `ThemeChanged` event; `VisualStateManagerExtensions.States` is plural.
- **Pick the layout panel by the language of the request.** CSS terms (`flex-grow`, `justify-content`, wrapping) mean `FlexPanel`; Figma auto-layout frames (negative spacing, absolute children) mean `AutoLayout`. `AutoLayout` cannot wrap; `FlexPanel` cannot use negative gaps. Equal flex columns need both `Grow="1"` and `Basis="0"`.
- **`ResponsiveExtension` is a markup extension** (`{utu:Responsive Narrow=..., Wide=...}`), never an element. `ResponsiveView` is the element form that swaps whole templates.
- **`ItemsRepeater` has no selection of its own.** `ItemsRepeaterExtensions` attached properties are the canonical way to add `SelectionMode`, `SelectedItem`, and incremental loading; pair with the `uno-mvux` skill for paginated feeds.
- **`SelectorExtensions.PipsPager` goes on the `FlipView`/`ListView`, not on the `PipsPager`**, and `NumberOfPages` is managed for you.
- **`ShadowContainer` inner shadows need `Background` on the container**, not on the child.
- **Prefer lightweight styling over custom styles.** Override `{ControlName}{State}{Property}` resource keys at app, page, or control scope (`ResourceExtensions`) instead of copying templates; the theme keeps working and dark mode stays correct.
- **`MaterialToolkitTheme` and `CupertinoToolkitTheme` replace the older `MaterialTheme` + `ToolkitResources` pair** and are mutually exclusive. They go in `App.xaml` `<Application.Resources>` and require `Toolkit` in `<UnoFeatures>`.
- **`ExtendedSplashScreen` on Android needs `Init()` in `MainActivity.OnCreate`**, and no `Region.Attached="True"` inside its content.

## Related skills

- `uno-navigation` for wiring `TabBar`, `NavigationBar`, and `DrawerControl` into routes, regions, and back navigation.
- `uno-themes` for the Material and Simple design systems, semantic brushes, and typography that Toolkit controls consume.
- `uno-mvux` for the feeds, states, and commands that `LoadingView`, `CommandExtensions`, and `ItemsRepeaterExtensions` bind to.
