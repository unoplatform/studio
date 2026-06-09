---
name: uno-toolkit-csharp-markup
description: "Use C# Markup with Uno Toolkit controls instead of XAML. Setup, NuGet packages, and fluent API patterns."
when_to_use: "Use when the project uses C# Markup instead of XAML and needs the Toolkit's fluent extension methods, setting up the `Uno.Toolkit.WinUI.Markup` NuGet package, applying Toolkit styles, extensions, and helpers in C# Markup, or converting existing XAML Toolkit usage to C# Markup."
metadata:
  author: uno-platform
  version: "2.4"
  category: toolkit
---

# Uno Toolkit C# Markup — Agent Skill

## Workflow

> **Docs lookup:** call `uno_platform_docs_search(...)` first, then `uno_platform_docs_fetch(sourcePath="…")` using the `sourcePath` field from a result (a relative `.md` path; add the result's `anchor` for a section). Never pass a URL, a `.html` link, or a hand-built path.

### Step 1: Fetch the C# Markup Toolkit Documentation

```
uno_platform_docs_search("Uno Toolkit C# Markup fluent API setup NuGet package")
```

Primary documentation pages:
- **Toolkit C# Markup**: `external/uno.toolkit.ui/doc/getting-started.md`
- **Material Toolkit C# Markup**: `external/uno.toolkit.ui/doc/material-getting-started.md`
- **Chefs C# Markup Toolkit Tutorial**: `external/uno.extensions/doc/Learn/Markup/HowTo-CustomMarkupProject-Toolkit.md`

Fetch the Chefs tutorial:

```
uno_platform_docs_fetch(sourcePath="external/uno.extensions/doc/Learn/Markup/HowTo-CustomMarkupProject-Toolkit.md")
```

## Key Principles (Stable)

- Add `Uno.Toolkit.WinUI.Markup` NuGet package
- For Material: add `Uno.Toolkit.WinUI.Material.Markup` NuGet package
- Fluent API: `new Card().Style(Theme.Card.Styles.Elevated)`
- Same controls and helpers, expressed in C# instead of XAML

## Related Skills

- [[uno-toolkit-getting-started]] — Base Toolkit setup
- [[uno-themes-material]] — Material C# Markup
