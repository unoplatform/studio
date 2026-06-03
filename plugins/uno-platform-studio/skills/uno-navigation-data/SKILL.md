---
name: uno-navigation-data
description: "Pass and receive data during navigation in Uno Platform applications."
when_to_use: "Use when passing data (objects, IDs, filters) from one page to another, receiving data in a ViewModel via constructor injection, returning results from a page back to the caller, using `NavigateDataAsync`, `NavigateBackWithResultAsync`, or setting up `DataViewMap` or `ResultDataViewMap` for data-based routes."
metadata:
  author: uno-platform
  version: "2.3"
  category: navigation
---

# Uno Navigation Data Passing — Agent Skill

## Workflow

> **Docs lookup:** call `uno_platform_docs_search(...)` first, then `uno_platform_docs_fetch(sourcePath="…")` using the `sourcePath` field from a result (a relative `.md` path; add the result's `anchor` for a section). Never pass a URL, a `.html` link, or a hand-built path.

### Step 1: Fetch the Data Passing Documentation

```
uno_platform_docs_search("Uno Navigation data passing receive NavigateDataAsync constructor injection")
```

Primary documentation pages:
- **Passing Navigation Data (Chefs)**: `external/uno.chefs/doc/navigation/PassingNavigationData.md`
- **Pass Data During Navigation**: `external/uno.extensions/doc/Learn/Navigation/Walkthrough/DisplayItemDetails.md`

Fetch the Chefs data passing page:

```
uno_platform_docs_fetch(sourcePath="external/uno.chefs/doc/navigation/PassingNavigationData.md")
```

### Step 2: For Route Configuration with Data

Data-based navigation requires proper route registration:

```
uno_platform_docs_search("Uno Navigation DataViewMap ResultDataViewMap route data type")
```

### Step 3: For Returning Results

If the user needs a round-trip (navigate, pick data, return):

The Chefs page covers `NavigateBackWithResultAsync` and `ResultDataViewMap` patterns.

### Step 4: For XAML Data Passing

For passing data declaratively:

```
uno_platform_docs_search("Uno Navigation XAML Navigation.Data binding pass")
```

## Key Principles (Stable)

- Data is received via constructor dependency injection in the target ViewModel
- `DataViewMap` associates a data type with a View/ViewModel for data-based navigation
- `ResultDataViewMap` additionally specifies a result type for round-trip navigation
- `NavigateBackWithResultAsync(data)` returns data to the calling page
- Routes must be configured in `RegisterRoutes` to support data types
- `Navigation.Data` attached property enables data passing in XAML

## Related Skills

- [[uno-navigation-routes]] — Route registration with data types
- [[uno-navigation-code]] — Programmatic navigation methods
- [[uno-navigation-xaml]] — XAML data binding for navigation
