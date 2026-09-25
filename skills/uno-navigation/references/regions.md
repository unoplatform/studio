# Uno Navigation Regions

## Workflow

> **Docs lookup:** call `uno_platform_docs_search(...)` first, then `uno_platform_docs_fetch(sourcePath="…")` using the `sourcePath` field from a result (a relative `.md` path; add the result's `anchor` for a section). Never pass a URL, a `.html` link, or a hand-built path.

### Step 1: Fetch the Regions Documentation

```
uno_platform_docs_search("Uno Navigation regions Region.Attached Region.Name navigator")
```

Primary documentation pages:
- **Define Regions Walkthrough**: `external/uno.extensions/doc/Learn/Navigation/Walkthrough/DefineRegions.md`
- **How-To: Define Regions**: `external/uno.extensions/doc/Learn/Navigation/HowTo-Regions.md`
- **Navigation Region Reference**: `external/uno.extensions/doc/Reference/Navigation/NavigationRegion.md`

Fetch the walkthrough:

```
uno_platform_docs_fetch(sourcePath="external/uno.extensions/doc/Learn/Navigation/Walkthrough/DefineRegions.md")
```

### Step 2: For Common Pitfalls

Fetch the how-to page for important warnings:

```
uno_platform_docs_fetch(sourcePath="external/uno.extensions/doc/Learn/Navigation/HowTo-Regions.md")
```

Critical warning: Do NOT use `Region.Attached="True"` inside Shell.xaml or ExtendedSplashScreen content.

### Step 3: For NavigationView with Regions

The walkthrough includes a section on "Using Regions with NavigationView".

### Step 4: For Visibility-Based Navigation  

If the user needs visibility-based content switching:

See the `references/panel-visibility.md`.

## Key Principles (Stable)

- `uen:Region.Attached="True"` — enables region-based navigation on a container
- `uen:Region.Name="RouteName"` — identifies which route this region represents
- `uen:Region.Navigator="Visibility"` — switches content by toggling visibility
- **CRITICAL**: Never use `Region.Attached="True"` inside Shell.xaml content — the navigation host isn't ready yet
- Regions mirror the navigation control hierarchy
- Navigation controls (TabBar, NavigationView) and content areas must share a common parent with `Region.Attached="True"`

## Related Skills

- `references/setup.md` — Navigation host configuration
- `references/panel-visibility.md` — Visibility-based navigation
- `references/navigationview.md` — NavigationView with regions
- `references/tabbar.md` — TabBar with regions
