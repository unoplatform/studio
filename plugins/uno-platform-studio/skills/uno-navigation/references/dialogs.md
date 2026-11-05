# Uno Navigation Dialogs

## Key Principles (Stable)

- Message dialogs are the simplest dialog type — title, message, and buttons via `navigator.ShowMessageDialogAsync<TResult>(this, ...)`, no route registration required; only the generic overload returns the choice
- Use the `!` qualifier prefix (`uen:Navigation.Request="!Filter"`) or `Qualifiers.Dialog` from code to open a `Page`-based dialog via navigation
- If the target is a `Page`, it displays as a flyout; `!` is what turns a `Page` into a flyout
- If the target is a `ContentDialog`, it displays as a modal, with or without `!`
- Dialog results can be returned via `NavigateBackWithResultAsync(this, data: value)` (or directly from `ShowMessageDialogAsync<TResult>` for the simple case)
- Dialogs participate in the navigation system — they have proper back-stack handling

## Docs, for what this page does not cover

Use these when the sections above do not answer the question: search, then fetch only the section you need, with the `anchor` from the search result. A whole page stays in context for the rest of the session.

### Fetch the Dialog Navigation Documentation

```
uno_platform_docs_search("Uno Navigation dialogs flyouts ContentDialog modal ShowMessageDialogAsync confirmation alert")
```

Primary documentation pages:
- **Display Dialogs as Flyouts or Modals**: `external/uno.extensions/doc/Learn/Navigation/Walkthrough/ShowDialog.md`
- **How-To: Display a Dialog**: `external/uno.extensions/doc/Learn/Navigation/HowTo-ShowDialog.md`

Fetch the walkthrough:

```
uno_platform_docs_fetch(sourcePath="external/uno.extensions/doc/Learn/Navigation/Walkthrough/ShowDialog.md", anchor="…")
```

### For Simple Message Dialogs

Use the generic `ShowMessageDialogAsync<TResult>` for the simplest case — a title, message, and buttons that return the user's choice. No route registration needed. The non-generic `ShowMessageDialogAsync(sender, ...)` returns plain `Task` and discards the choice.

```csharp
var choice = await navigator.ShowMessageDialogAsync<string>(this,
    title: "Delete?",
    content: "This cannot be undone.",
    buttons: [new DialogAction("Delete", Id: "delete"), new DialogAction("Cancel", Id: "cancel")]);
if (choice == "delete") { ... }
```

The result is the chosen `DialogAction.Id`, or its `Label` when `Id` is null.

### For Flyout vs Modal

- **Flyout**: navigation target is a `Page` → displayed as flyout (needs the `!` qualifier)
- **Modal**: navigation target is a `ContentDialog` → displayed as modal even without `!`; the resolver applies `Qualifiers.Dialog` to any `ContentDialog` route

### For Dialog Results

If the user needs to return data from a dialog:

See the `references/data.md` for `NavigateBackWithResultAsync`.

## A picker page that returns a row

A lookup field (a contact's account, an order's customer) opens a page over the related table, with search and paging, and gets the chosen row back. **Never use a `ComboBox` over the whole table** for this: it loads every row and has no search.

**Register the picker by its result type**, once per app, however many forms use it:

```csharp
new ResultDataViewMap<AccountPickerPage, AccountPickerModel, AccountPick>()
new RouteMap("AccountPicker", View: views.FindByViewModel<AccountPickerModel>())
```

`AccountPick` is a small record with the key and the display name.

**The form awaits the row and opens the picker as a flyout:**

```csharp
public async ValueTask PickAccount(CancellationToken ct)
{
    if (await _navigator.GetDataAsync<AccountPick>(this, qualifier: Qualifiers.Dialog, cancellation: ct) is { } pick)
    {
        await Draft.UpdateAsync(d => d is null ? d : d with { AccountId = pick.Id, AccountName = pick.Name }, ct);
    }
}

public async ValueTask ClearAccount(CancellationToken ct) =>
    await Draft.UpdateAsync(d => d is null ? d : d with { AccountId = null, AccountName = null }, ct);
```

`GetDataAsync<TResult>` resolves the route from the result type and returns `default` when the picker closes without a row, so Cancel changes nothing. Clear belongs to the form, beside the Pick button.

**The picker returns the row from XAML.** A row click and Cancel both navigate back; the list sends the clicked item as the result:

```xml
<ListView ItemsSource="{Binding Data}" IsItemClickEnabled="True" SelectionMode="None"
          uen:Navigation.Request="-" />
<Button Content="Cancel" uen:Navigation.Request="-" />
```

The list is a paginated MVUX list feed with server-side search, debounced (`uno-mvux` skill, `references/pagination.md`), inside a `FeedView` with loading, empty and error templates.

## Related Skills

- `references/qualifiers.md` — Qualifier prefixes
- `references/data.md` — Passing/returning data
- `references/code.md` — Programmatic navigation
