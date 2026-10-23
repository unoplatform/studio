# Uno Toolkit AncestorBinding

## Key Principles (Stable)

- `{utu:AncestorBinding AncestorType=MyControl, Path=DataContext.MyProperty}` — binds to an ancestor's DataContext
- `{utu:ItemsControlBinding Path=DataContext.MyCommand}` — shorthand for binding to the parent ItemsControl's DataContext
- Similar to WPF's `{RelativeSource Mode=FindAncestor, AncestorType=...}`
- Essential for commands in DataTemplates that need to call the page's ViewModel
- XAML namespace: `xmlns:utu="using:Uno.Toolkit.UI"`

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
