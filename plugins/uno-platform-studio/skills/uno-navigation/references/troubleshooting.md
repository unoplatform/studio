# Uno Navigation Troubleshooting

## Workflow

### Step 1: Search for Navigation Troubleshooting

```
uno_platform_docs_search("Uno Navigation troubleshooting errors common issues")
```

### Step 2: Check Common Pitfalls

Fetch the regions how-to page for critical warnings:

```
uno_platform_docs_fetch(sourcePath="external/uno.extensions/doc/Learn/Navigation/HowTo-Regions.md")
```

### Step 3: For Route Registration Issues

```
uno_platform_docs_search("Uno Navigation route not found register ViewMap DataViewMap")
```

### Step 4: For Advanced Navigation Issues

```
uno_platform_docs_fetch(sourcePath="external/uno.extensions/doc/Learn/Navigation/Advanced/HowTo-AdvancedPageNavigation.md")
```

## Common Issues (Stable Reference)

1. **Region.Attached in Shell.xaml**: NEVER use `Region.Attached="True"` inside Shell.xaml or ExtendedSplashScreen content — navigation host isn't ready yet
2. **Missing route registration**: All pages must be registered via `ViewMap`/`DataViewMap` in `RegisterRoutes`
3. **Missing UnoFeatures**: Ensure `Navigation;Toolkit` are in `<UnoFeatures>`
4. **Missing XAML namespace**: `xmlns:uen="using:Uno.Extensions.Navigation.UI"` required for regions and attached properties
5. **Back stack issues**: Use qualifiers (`-/` or `Qualifiers.ClearBackStack`) to manage back stack
6. **Data not arriving**: Ensure `DataViewMap` is registered for the data type, and ViewModel accepts data via constructor
7. **`Region.Navigator="Visibility"` does nothing**: it works only on a `Grid`; on any other panel the region cannot navigate, so the request bubbles up to the parent region and the shell Frame navigates the whole page instead of the content area. Nothing is logged: the navigator's control is null and `RegionCanNavigate` just returns false
8. **TabBar renders unstyled, or `XamlParseException: Cannot find a Resource with the Name/Key BottomTabBarStyle` on the Windows head**: the alias keys exist only in the Material and Simple toolkit themes; on WebAssembly and Skia a missing key resolves to null with no error. Remove the `Style` attributes on Fluent (`-preset blank`), or use the `Cupertino*` keys on Cupertino. A null page background has the same cause: `BackgroundBrush` is Material/Simple only, so the page needs `ApplicationPageBackgroundThemeBrush` (Fluent) or `CupertinoSystemBackgroundBrush` (Cupertino)
9. **Settings never highlights in NavigationView**: the built-in `SettingsItem` is not enumerated by the navigator. Set `IsSettingsVisible="False"` and add a `NavigationViewItem` with `uen:Region.Name="Settings"` to `FooterMenuItems`

## Related Skills

- `references/setup.md` — Proper setup
- `references/regions.md` — Region configuration
- `references/routes.md` — Route registration
