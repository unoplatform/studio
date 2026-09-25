# MVUX Selection

## Workflow

> **Docs lookup:** call `uno_platform_docs_search(...)` first, then `uno_platform_docs_fetch(sourcePath="…")` using the `sourcePath` field from a result (a relative `.md` path; add the result's `anchor` for a section). Never pass a URL, a `.html` link, or a hand-built path.

### Step 1: Fetch the Selection Documentation

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
uno_platform_docs_fetch(sourcePath="external/uno.extensions/doc/Learn/Mvux/Advanced/Selection.md")
```

### Step 2: For How-To Walkthroughs

For step-by-step examples:

```
uno_platform_docs_fetch(sourcePath="external/uno.extensions/doc/Learn/Mvux/Walkthrough/Selection.howto.md")
```

This covers single selection, multi selection, checking selection from commands, and programmatic selection.

### Step 3: Understand Selection Patterns

From the fetched docs:
- **Single selection**: `.Selection(selectedState)` where `selectedState` is `IState<T?>`
- **Multi-selection**: `.Selection(selectedStates)` where `selectedStates` is `IState<IImmutableList<T>>`
- **Manual selection**: Use `IListState<T>` instead of `IListFeed<T>` for programmatic item selection

### Step 4: For Selection with Commands

If the user needs to use the selected item in a command (e.g., "Edit" button):

```
uno_platform_docs_search("MVUX selection command check current selected item button")
```

## Key Principles (Stable)

- Use `.Selection(state)` operator on `IListFeed<T>` or `IListState<T>` to enable selection tracking
- Single selection: `IState<T?>` holds the selected item
- Multi-selection: `IState<IImmutableList<T>>` holds selected items
- The `ListView` SelectionMode must be set appropriately (Single, Multiple, Extended)
- Selection sync happens automatically between the UI control and the state
- For programmatic/manual selection, use `IListState<T>` (not `IListFeed<T>`)

## Related Skills

- `references/listfeed.md` — Read-only collections with selection
- `references/liststate.md` — Mutable collections with selection
- `references/commands.md` — Commands that use selected items
