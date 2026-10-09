# Uno Toolkit C# Markup

## Workflow

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

- Add `Uno.Toolkit.WinUI.Markup` NuGet package, then in `App.cs`: `using Uno.Toolkit.UI.Markup;` and `this.Build(r => r.UseToolkit());`
- For Material: add `Uno.Toolkit.WinUI.Material.Markup` NuGet package (it includes the base Markup package)
- Fluent API: `new CardContentControl().Style(ToolkitTheme.CardContentControl.Styles.Elevated)` (`Uno.Toolkit.UI.Markup.ToolkitTheme`)
- Same controls and helpers, expressed in C# instead of XAML

## Related Skills

- `references/getting-started.md` — Base Toolkit setup
- the `uno-themes` skill (`references/material.md`) — Material C# Markup
