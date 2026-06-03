---
name: uno-navigation-troubleshooting
description: "Troubleshoot common issues with Uno Platform Navigation Extensions."
when_to_use: "Use when navigation is not working or throwing errors, pages not loading or blank screens, back navigation not working as expected, region-based navigation issues, route registration errors, or shell.xaml / ExtendedSplashScreen initialization issues."
metadata:
  author: uno-platform
  version: "2.3"
  category: navigation
---

# Uno Navigation Troubleshooting — Agent Skill

## Workflow

> **Docs lookup:** call `uno_platform_docs_search(...)` first, then `uno_platform_docs_fetch(sourcePath="…")` using the `sourcePath` field from a result (a relative `.md` path; add the result's `anchor` for a section). Never pass a URL, a `.html` link, or a hand-built path.

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

## Related Skills

- [[uno-navigation-setup]] — Proper setup
- [[uno-navigation-regions]] — Region configuration
- [[uno-navigation-routes]] — Route registration
