# MVUX Selection

## Key Principles (Stable)

- Use `.Selection(state)` operator on `IListFeed<T>` or `IListState<T>` to enable selection tracking
- Single selection: `IState<T>` holds the selected item
- Multi-selection: `IState<IImmutableList<T>>` holds selected items
- The `ListView` SelectionMode must be set appropriately (Single or Multiple)
- Selection sync happens automatically between the UI control and the state
- For programmatic/manual selection, use `IListState<T>` (not `IListFeed<T>`)

## Understand Selection Patterns

- **Single selection**: `.Selection(selectedState)` where `selectedState` is `IState<T>` (the docs declare `IState<Person>`; a `T?` annotation compiles but only adds nullable warnings)
- **Multi-selection**: `.Selection(selectedStates)` where `selectedStates` is `IState<IImmutableList<T>>`
- **Manual selection**: Use `IListState<T>` instead of `IListFeed<T>` for programmatic item selection: `TrySelectAsync(item)`, `TryDeselectAsync(item)`, `ClearSelectionAsync()`, documented in `external/uno.extensions/doc/Learn/Mvux/ListStates.md` ("Selection operators")

## Docs, for what this page does not cover

Use these when the sections above do not answer the question: search, then fetch only the section you need, with the `anchor` from the search result. A whole page stays in context for the rest of the session.

### Fetch the Selection Documentation

Search for and fetch the selection documentation:

```
uno_platform_docs_search("MVUX selection Selection method IListFeed IListState")
```

Primary documentation pages:
- **Selection Reference**: `external/uno.extensions/doc/Learn/Mvux/Advanced/Selection.md`
- **Selection How-To**: `external/uno.extensions/doc/Learn/Mvux/Walkthrough/Selection.howto.md`
- **Selection in Chefs App**: `external/uno.chefs/doc/mvux/Selection.md`

Fetch the reference page:

```
uno_platform_docs_fetch(sourcePath="external/uno.extensions/doc/Learn/Mvux/Advanced/Selection.md", anchor="…")
```

### For How-To Walkthroughs

For step-by-step examples:

```
uno_platform_docs_fetch(sourcePath="external/uno.extensions/doc/Learn/Mvux/Walkthrough/Selection.howto.md", anchor="…")
```

This covers single selection, multi selection, checking selection from commands, and programmatic selection.

### For Selection with Commands

If the user needs to use the selected item in a command (e.g., "Edit" button):

```
uno_platform_docs_search("MVUX selection command check current selected item button")
```

## Related Skills

- `references/listfeed.md` — Read-only collections with selection
- `references/liststate.md` — Mutable collections with selection
- `references/commands.md` — Commands that use selected items
