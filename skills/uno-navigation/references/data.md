# Uno Navigation Data Passing

## Workflow

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
- `NavigateBackWithResultAsync(this, data: value)` returns data to the calling page (`sender` comes first)
- Routes must be configured in `RegisterRoutes` to support data types
- `Navigation.Data` attached property enables data passing in XAML

## Related Skills

- `references/routes.md` — Route registration with data types
- `references/code.md` — Programmatic navigation methods
- `references/xaml.md` — XAML data binding for navigation
