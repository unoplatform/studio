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

Use `navigator.ShowMessageDialogAsync(...)` for the simplest case — a title, message, and buttons that return the user's choice. No route registration needed.

### Step 3: For Flyout vs Modal

- **Flyout**: navigation target is a `Page` → displayed as flyout
- **Modal**: navigation target is a `ContentDialog` → displayed as modal

### Step 4: For Dialog Results

If the user needs to return data from a dialog:

See the `references/data.md` for `NavigateBackWithResultAsync`.

## Key Principles (Stable)

- Message dialogs are the simplest dialog type — title, message, and buttons via `navigator.ShowMessageDialogAsync(...)`, no route registration required
- Use `!/` qualifier prefix or `Qualifiers.Dialog` to open a `Page`/`ContentDialog`-based dialog via navigation
- If the target is a `Page`, it displays as a flyout
- If the target is a `ContentDialog`, it displays as a modal
- Dialog results can be returned via `NavigateBackWithResultAsync` (or directly from `ShowMessageDialogAsync` for the simple case)
- Dialogs participate in the navigation system — they have proper back-stack handling

## Related Skills

- `references/qualifiers.md` — Qualifier prefixes
- `references/data.md` — Passing/returning data
- `references/code.md` — Programmatic navigation
