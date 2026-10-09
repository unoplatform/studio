# Uno Navigation Qualifiers

## Workflow

### Step 1: Fetch the Qualifiers Documentation

```
uno_platform_docs_search("Uno Navigation qualifiers back stack clear dialog Qualifiers class")
```

Primary documentation pages:
- **Navigation Qualifiers Reference**: `external/uno.extensions/doc/Reference/Navigation/Qualifiers.md`
- **Advanced Page Navigation**: `external/uno.extensions/doc/Learn/Navigation/Walkthrough/AdvancedPageNavigation.md`
- **XAML Navigation Qualifiers (Chefs)**: `external/uno.chefs/doc/navigation/XamlNavigation.md`

Fetch the qualifiers reference:

```
uno_platform_docs_fetch(sourcePath="external/uno.extensions/doc/Reference/Navigation/Qualifiers.md")
```

### Step 2: For Advanced Back Stack Management

Fetch the advanced page navigation walkthrough:

```
uno_platform_docs_fetch(sourcePath="external/uno.extensions/doc/Learn/Navigation/Walkthrough/AdvancedPageNavigation.md")
```

### Step 3: For Code-Behind Qualifiers

If using qualifiers from code:

```
uno_platform_docs_search("Uno Navigation Qualifiers.ClearBackStack code behind NavigateViewModelAsync")
```

Key page:
- **How-To Advanced Page Navigation**: `external/uno.extensions/doc/Learn/Navigation/Advanced/HowTo-AdvancedPageNavigation.md`

## Key Principles (Stable)

- `-/` prefix: clears back stack (e.g., `uen:Navigation.Request="-/Login"`)
- `!` prefix: opens a dialog/flyout (`!Route`)
- Qualifiers can be combined with route names
- From code: use `Qualifiers.ClearBackStack`, `Qualifiers.Dialog`, etc.
- `Qualifiers` class is in `Uno.Extensions.Navigation` namespace
- Common scenario: after login, navigate to main page with `-/` to prevent back navigation to login

## Related Skills

- `references/code.md` — Programmatic navigation
- `references/xaml.md` — XAML navigation with qualifiers
- `references/dialogs.md` — Dialog navigation
