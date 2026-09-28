# Uno Navigation Dialogs

## Workflow

> **Docs lookup:** call `uno_platform_docs_search(...)` first, then `uno_platform_docs_fetch(sourcePath="…")` using the `sourcePath` field from a result (a relative `.md` path; add the result's `anchor` for a section). Never pass a URL, a `.html` link, or a hand-built path.

### Step 1: Fetch the Dialog Navigation Documentation

```
uno_platform_docs_search("Uno Navigation dialogs flyouts ContentDialog modal ShowMessageDialogAsync confirmation alert")
```

Primary documentation pages:
- **Display Dialogs as Flyouts or Modals**: `external/uno.extensions/doc/Learn/Navigation/Walkthrough/ShowDialog.md`
- **How-To: Display a Dialog**: `external/uno.extensions/doc/Learn/Navigation/HowTo-ShowDialog.md`

Fetch the walkthrough:

```
uno_platform_docs_fetch(sourcePath="external/uno.extensions/doc/Learn/Navigation/Walkthrough/ShowDialog.md")
```

### Step 2: For Simple Message Dialogs

Use the generic `ShowMessageDialogAsync<TResult>` for the simplest case — a title, message, and buttons that return the user's choice. No route registration needed. The non-generic `ShowMessageDialogAsync(sender, ...)` returns plain `Task` and discards the choice.

```csharp
var choice = await navigator.ShowMessageDialogAsync<string>(this,
    title: "Delete?",
    content: "This cannot be undone.",
    buttons: [new DialogAction("Delete", Id: "delete"), new DialogAction("Cancel", Id: "cancel")]);
if (choice == "delete") { ... }
```

### Step 3: For Flyout vs Modal

- **Flyout**: navigation target is a `Page` → displayed as flyout (needs the `!` qualifier)
- **Modal**: navigation target is a `ContentDialog` → displayed as modal even without `!`; the resolver applies `Qualifiers.Dialog` to any `ContentDialog` route

### Step 4: For Dialog Results

If the user needs to return data from a dialog:

See the `references/data.md` for `NavigateBackWithResultAsync`.

## Key Principles (Stable)

- Message dialogs are the simplest dialog type — title, message, and buttons via `navigator.ShowMessageDialogAsync<TResult>(this, ...)`, no route registration required; only the generic overload returns the choice
- Use the `!` qualifier prefix (`uen:Navigation.Request="!Filter"`, the documented form; `!/Filter` also parses) or `Qualifiers.Dialog` from code to open a `Page`-based dialog via navigation
- If the target is a `Page`, it displays as a flyout; `!` is what turns a `Page` into a flyout
- If the target is a `ContentDialog`, it displays as a modal, with or without `!`
- Dialog results can be returned via `NavigateBackWithResultAsync(this, data: value)` (or directly from `ShowMessageDialogAsync<TResult>` for the simple case)
- Dialogs participate in the navigation system — they have proper back-stack handling

## Related Skills

- `references/qualifiers.md` — Qualifier prefixes
- `references/data.md` — Passing/returning data
- `references/code.md` — Programmatic navigation
