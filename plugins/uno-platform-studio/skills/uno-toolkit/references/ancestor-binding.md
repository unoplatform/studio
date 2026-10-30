# Uno Toolkit AncestorBinding

## Key Principles (Stable)

- `{utu:AncestorBinding AncestorType=MyControl, Path=DataContext.MyProperty}` — binds to an ancestor's DataContext
- `{utu:ItemsControlBinding Path=DataContext.MyCommand}` — shorthand for `AncestorType=ItemsControl`
- Replaces WPF's `{RelativeSource Mode=FindAncestor, AncestorType=...}`, which does not exist in WinUI: `RelativeSource` supports only `Self` and `TemplatedParent`
- Essential for commands in DataTemplates that need to call the page's ViewModel
- Properties: `AncestorType`, `Path` (resolved **on the ancestor**), `Converter`, `ConverterParameter`, `ConverterLanguage`
- XAML namespace: `xmlns:utu="using:Uno.Toolkit.UI"`

## Pick the escape by template scope

When the page's `DataContext` is its model (the usual MVUX and Uno Navigation setup), `AncestorType=Page` reaches it from any template.

| Inside | Use | Why |
|---|---|---|
| `ListView`/`GridView` item template, list **not** in a `FeedView` | `{utu:ItemsControlBinding Path=DataContext.X}` | The list's `DataContext` is the page model |
| `ListView`/`GridView` item template inside a `FeedView` `ValueTemplate` | `{utu:AncestorBinding AncestorType=Page, Path=DataContext.X}` | Inside the `ValueTemplate` the list's `DataContext` is the `FeedViewState`, so `ItemsControlBinding` binds nothing |
| `ItemsRepeater` item template | `{utu:AncestorBinding AncestorType=Page, Path=DataContext.X}` | `ItemsRepeater` derives from `FrameworkElement`, not `ItemsControl`: `ItemsControlBinding` walks past it |
| `ContentControl.ContentTemplate` or another nested template | `{utu:AncestorBinding AncestorType=Page, Path=DataContext.X}` | Reaches the page model directly |
| The wrapping control's `Content` | `{utu:AncestorBinding AncestorType=ContentControl, Path=Content.X}` | When `Content` holds the item |

## Common Pitfalls

1. **Leaving out `DataContext.`** `Path=X` binds to a property `X` of the ancestor *element* (the `Page`, the `ListView`), which almost never exists.
2. **Too generic an `AncestorType`.** `AncestorType=Grid` stops at the nearest inner `Grid`, not the page. Name `Page`, `ItemsControl`, `ContentControl` or the specific list type.

## Docs, for what this page does not cover

Use these when the sections above do not answer the question: search, then fetch only the section you need, with the `anchor` from the search result. A whole page stays in context for the rest of the session.

### Fetch the AncestorBinding Documentation

```
uno_platform_docs_search("Uno Toolkit AncestorBinding ItemsControlBinding relative binding DataTemplate ancestor")
```

Primary documentation page:
- **AncestorBinding & ItemsControlBinding**: `external/uno.toolkit.ui/doc/helpers/ancestor-itemscontrol-binding.md`

Fetch the page:

```
uno_platform_docs_fetch(sourcePath="external/uno.toolkit.ui/doc/helpers/ancestor-itemscontrol-binding.md", anchor="…")
```

## Related Skills

- `references/command-extensions.md` — Commands in templates
