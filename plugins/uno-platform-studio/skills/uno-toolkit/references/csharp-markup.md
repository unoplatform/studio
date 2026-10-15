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

- `<UnoFeatures>Toolkit;CSharpMarkup</UnoFeatures>` brings in the `Uno.Toolkit.WinUI.Markup` package; then in `App.xaml.cs`: `using Uno.Toolkit.UI.Markup;` and `Resources.Build(r => r.UseToolkit());` (`Build` extends `ResourceDictionary`, not `Application`)
- For Material: adding `Material` brings in `Uno.Toolkit.WinUI.Material.Markup` (it includes the base Markup package); merge with `Resources.Build(r => r.UseMaterialToolkit())` (`using Uno.Toolkit.UI.Material.Markup;`)
- Fluent API: `new CardContentControl().Style(ToolkitTheme.CardContentControl.Styles.Elevated)` (`Uno.Toolkit.UI.Markup.ToolkitTheme`)
- Same controls and helpers, expressed in C# instead of XAML

## Related Skills

- `references/getting-started.md` — Base Toolkit setup
- the `uno-themes` skill (`references/material.md`) — Material C# Markup
