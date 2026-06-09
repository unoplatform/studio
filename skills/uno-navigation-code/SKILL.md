---
name: uno-navigation-code
description: "Programmatic navigation via `INavigator` extension methods (`NavigateRouteAsync`, `NavigateViewModelAsync`, `NavigateBackAsync`, and the `*ForResultAsync` overloads)."
when_to_use: "Use when navigating from a ViewModel, code-behind, or a service, including back-navigation (`NavigateBackAsync`), result-returning navigation (`NavigateViewModelForResultAsync<TViewModel, TResult>`, `NavigateBackWithResultAsync`), or choosing the right `Navigate*Async` overload for the scenario."
metadata:
  author: uno-platform
  version: "2.4"
  category: navigation
---

# Uno Navigation from Code — Agent Skill

## Workflow

> **Docs lookup:** call `uno_platform_docs_search(...)` first, then `uno_platform_docs_fetch(sourcePath="…")` using the `sourcePath` field from a result (a relative `.md` path; add the result's `anchor` for a section). Never pass a URL, a `.html` link, or a hand-built path.

### Step 1: Fetch the Code Navigation Documentation

```
uno_platform_docs_search("Uno Navigation code programmatic NavigateRouteAsync NavigateViewModelAsync")
```

Primary documentation pages:
- **Navigation via Code Behind (Chefs)**: `external/uno.chefs/doc/navigation/NavigationCodeBehind.md`
- **Navigation Overview (Code section)**: `external/uno.extensions/doc/Learn/Navigation/NavigationOverview.md`

Fetch the Chefs code-behind page:

```
uno_platform_docs_fetch(sourcePath="external/uno.chefs/doc/navigation/NavigationCodeBehind.md")
```

### Step 2: For Specific Navigation Methods

The documentation covers two parallel families:

**Non-result overloads** — navigate to a destination, don't expect a return value:
- `NavigateRouteAsync("routeName")` — navigate by route string
- `NavigateViewAsync<TView>()` — navigate to a specific view type
- `NavigateViewModelAsync<TViewModel>()` — navigate to the route registered for a ViewModel type
- `NavigateDataAsync<TData>(data)` — navigate with data; the destination is resolved from the data type
- `NavigateBackAsync()` — pop one level off the back stack

**Result overloads** — navigate AND retrieve a typed result from the destination page:
- `NavigateRouteForResultAsync<TResult>("routeName")`
- `NavigateViewForResultAsync<TView, TResult>()`
- `NavigateViewModelForResultAsync<TViewModel, TResult>()`
- `NavigateDataForResultAsync<TData, TResult>()`
- `NavigateBackWithResultAsync(data)` — used on the destination page to return data to the caller

The result overloads return `Task<NavigationResult<TResult>>`. The caller awaits, then reads `.Result` for the returned value.

### Step 3: For Advanced Navigation Techniques

```
uno_platform_docs_search("Uno Navigation advanced back stack clear navigate techniques")
```

Key page:
- **Advanced Page Navigation**: `external/uno.extensions/doc/Learn/Navigation/Walkthrough/AdvancedPageNavigation.md`

## Critical Rules

- **Pick the correct overload for the navigation scenario.** Non-result overloads (`NavigateViewModelAsync<TViewModel>`) are for one-way navigation. Result overloads (`NavigateViewModelForResultAsync<TViewModel, TResult>`) are for request-and-await-response flows where the destination page calls `NavigateBackWithResultAsync(data)` to return a value. Using the non-result overload when a return value is needed silently throws away the result.
- Navigation methods are **extension methods on `INavigator`** — they are not extension methods on `string`, `Route`, or any view type. Get an `INavigator` instance first (constructor injection or `this.GetNavigator()`), then call `await navigator.Navigate*Async(...)`.

## Key Principles (Stable)

- Get `INavigator` via constructor injection or `this.GetNavigator()` extension method
- All navigation methods are async; non-result overloads return `Task<NavigationResponse>`, result overloads return `Task<NavigationResultResponse<TResult>>`
- `NavigateViewModelAsync` is the most common one-way overload — maps to the route registered for that ViewModel
- `NavigateBackAsync()` goes back one level
- Combine with `Qualifiers.ClearBackStack` to clear navigation history
- Always `await` navigation calls

## Common Pitfall Example

The second Critical Rule above (extension methods on `INavigator`) is the most-seen mistake. Concrete example:

```csharp
// WRONG: CS1929 — string has no NavigateRouteAsync
await "/details".NavigateRouteAsync(...);

// CORRECT: Get INavigator first
var navigator = this.GetNavigator();
await navigator.NavigateRouteAsync(this, route: "/details");
```

In XAML, prefer the `uen:Navigation.Request` attached property instead of code-behind navigation:

```xml
<Button Content="Go to Details"
        uen:Navigation.Request="Details"/>
```

## Related Skills

- [[uno-navigation-routes]] — Route registration (required for code navigation)
- [[uno-navigation-data]] — Passing data during navigation
- [[uno-navigation-qualifiers]] — Navigation qualifiers (clear back stack, etc.)
- [[uno-navigation-xaml]] — Declarative alternative to code navigation
