# Recipes

A recipe is a small complete implementation of one user-visible behaviour: view, presentation state, service, registration, navigation, failure handling and tests. Recipes exist because the defects that hurt real apps are in the wiring a snippet leaves out: a setting that never saves, a button with no command, a detail page bound to a stale copy of its data.

**Status.** This file supplies acceptance contracts, not runnable reference implementations. Use the contracts below for a matching feature. If consulting a sample such as [Uno Chefs](https://github.com/unoplatform/uno.chefs), verify its current source and tests before treating it as a recipe implementation.

## Catalogue

### `persisted-settings`

Changing a preference updates the display and persists through the app's settings service. A new session restores it. A failed save leaves a clear, recoverable state. Rapid edits cannot let an older save win.

Accept when: the documented default shows on first launch with no write during loading; a toggle reaches the service and the view settles on the saved value; the value survives closing and relaunching the app; a forced write failure shows an unsaved or error state and retry recovers; rapid false, true, false ends with false both displayed and stored; navigating away and back creates no duplicate subscription or write; keyboard activation behaves like a click.

### `list-detail-mutation`

A list opens a detail page. Changing an item (for example, marking it favourite) updates the detail page and every list that shows the item. A navigation payload identifies or seeds the item; it is not the long-lived source of its state.

A record page lists the records of each table that points at it (a company's contacts, a contact's activities) as paged lists whose rows open the related record. Delete asks first, removes the row from every list that shows it, and shows the backend's refusal when it refuses.

Accept when: each related list shows its rows and opens one; a confirmed delete removes the record everywhere and a cancelled one changes nothing; the detail page updates from live state after the change; the list reflects it on return without a restart; another view showing the same item updates; a filtered list (such as favourites only) gains or loses the item and its count updates; a failed change shows no false confirmed state; two similar items are told apart by identity; revisiting the detail page does not multiply handlers; the UI uses the entity the service returned, not the request.

### `search-filter-results`

Query and filter changes update results consistently. Loading, empty, error and retry are visible. A slow older request cannot overwrite results for a newer query.

Over server data, the query, the filter and the sort go to the server and the results are a paginated feed (`uno-mvux`, `references/pagination.md`): the list covers every row of its table the user may see, not the rows another page happened to load. A count shown beside a filter, a tab or a heading is the server's count for that filter, and a total is a server aggregate; neither is counted from the rows loaded so far. This holds whatever the row count is today (rule 13 of the skill).

Every list has four states, each with its own content (rule 15; `uno-mvux`, `references/feedview.md`): loading; empty, which says what is missing and offers the add action; no match, which names the search and offers Clear; error, which says what failed, with Retry. That is every list: a dashboard's, a record's related lists, a picker's and a flyout's too (one that cannot be searched has no no-match state). A table-like list has a header row naming its columns; a sortable header, or a sort menu when the window is narrow, shows the current sort and sends a new one to the server, and the list restarts from its first page. A column that shows another row's name (a contact's account) sorts by that name, never by the id behind it, with sample data as with the server.

Accept when: an empty table and a search with no match show different texts, and Clear restores the list; Retry reruns the request on every list, not only the first; choosing a sort sends a sorted request and the header shows its direction; a sort on a column that shows a related row puts its names in alphabetical order, with sample data and with the server; F5 reloads the list; matches and count agree; over server data, typing a query sends a request and scrolling to the end fetches the next page; a record that no other page has loaded is found by search; no-match shows an empty state, not stale results; retry after an error reruns the current query; a slow query A followed by a fast query B never shows A's results; cancelling a filter edit leaves applied results unchanged; clear restores defaults; leaving mid-request causes no error; retrying the same query after a failure is not suppressed as "unchanged".

### `actionable-card`

A card opens its item. A secondary action on the card (favourite, vote) acts on the same item without navigating. Both work by keyboard and assistive technology.

Accept when: activating the card navigates once, to the right item; the secondary action mutates once and does not navigate; keyboard focus order and activation match pointer behaviour; the secondary action's state updates in the rendered card; disabled and pending states agree with behaviour; a recycled card acts on its current item, not a previous one; actions are visible and unclipped in light, dark, narrow and wide.

### `theme-and-brand`

The app keeps its brand while rendering readable, consistent controls in each supported theme and window size. Theme switching goes through the existing theme service.

Accept when: text is readable on its actual surfaces in each supported theme; existing or requested theme switching updates without a restart; system-theme following and persisted explicit choices work when in scope; nothing clips at applicable narrow or wide sizes; custom cards and overlays pair foreground and background deliberately; a legacy theme keeps its initialisation. New shared styles use semantic keys supported by the selected theme and resolved version. Do not add a theme selector or migrate an existing theme to satisfy this recipe. See [design.md](design.md).

### `dialog-result`

A page opens a modal editor or filter. Apply returns a typed result and updates the caller once. Cancel or dismiss commits nothing.

Accept when: the dialog opens with the current values as a draft; apply updates the caller exactly once; cancel leaves the caller unchanged; back, Escape or tapping outside behave as documented cancel; an invalid draft keeps the dialog open with an accessible error; reopening starts from committed values; rapid repeated opening does not stack dialogs; keyboard focus stays in the dialog and returns to the caller.

### `edit-form`

A page or dialog edits a record through a draft. Save validates, persists and returns; Cancel discards. The form works from the keyboard alone, on a phone as on a desktop.

**The field list comes from the backend, not from the mock-up alone.** A mock-up shows one table's form; every other table with a form needs its fields too. When the backend publishes a form definition or column metadata (a Dataverse table's main form and its attribute metadata: the `uno-kiota` skill's Dataverse reference), the form shows those fields in those groups, each with the rule the metadata gives it: required, length, range, format. Without such a source, the form shows every column a user may edit, grouped by meaning, and the report says the list was inferred. A form that edits three columns of a table that has fifteen editable ones is unfinished.

**A field whose value is a row of another table is a picker, not a `ComboBox`.** An opportunity's account, a contact's company and a record's parent each open a page or dialog over that table, with server search and paging, that returns the chosen row; the field shows the chosen row's name and offers Clear when it is optional (`uno-navigation`, `references/dialogs.md`, *A picker page that returns a row*). One picker per target table, registered once and reused by every form. A `ComboBox` is for a fixed set of choices (a status, a category), filled from the backend's choice metadata when it has one. How few rows the table holds today does not change this (rule 13 of the skill).

**Keyboard and leaving.** Each shortcut sits on the button that owns the action, as a `KeyboardAccelerator` (`Modifiers="Control" Key="S"` on Save, `Key="Escape"` on Cancel or Close, `Key="F5"` on Refresh), so the key and the button cannot disagree. A Save that validation refuses shows a summary of the errors above the fields, and the view moves focus to the first invalid field (`Focus(FocusState.Programmatic)`; focus is the view's concern). The model knows whether the draft differs from the record, and every way out (Cancel, Escape, back, another row, another section) passes one check that asks before discarding (`uno-navigation`, `references/dialogs.md`).

Accept when: Ctrl+S saves and Escape leaves as the buttons do; a refused Save lists the errors and focus lands on the first invalid field; leaving a changed draft asks first, Discard discards and Keep editing returns to the draft unchanged; an unchanged draft leaves without a question; the form opens on a copy of the record, not the live one; its fields match the backend's form or column metadata, or the report lists what was left out and why; a lookup field opens its picker, typing there searches the server, and the chosen row comes back into the draft; each invalid field shows its message beside it and Save stays unavailable or reports why; Enter or the on-screen keyboard's Next moves to the next field and the last field's Done submits or dismisses the keyboard; the on-screen keyboard never covers the focused field or the Save action; a failed save keeps the draft and shows a recoverable error; Save cannot run twice while one is pending; Cancel and back leave the record unchanged. Load `uno-mvux` (`references/validation.md`) for the draft and its errors, and `uno-toolkit` (`references/input-extensions.md`) for the field-to-field keyboard flow.

### `attach-file`

The user picks a photo or file to attach to a record. The attachment is still there after the app restarts.

Accept when: the system picker opens on every target and cancelling it changes nothing; the picked file is copied into app storage and the record keeps that copy, not the picker's path; the attachment shows after a relaunch; a failed copy shows an error and attaches nothing.

`Windows.Storage.Pickers.FileOpenPicker` works on Desktop, Android, iOS and WebAssembly. Add at least one `FileTypeFilter` entry (`"*"` allows any file) and set `SuggestedStartLocation`, or WinUI throws. On the Windows App SDK head only, initialise it with the window handle: `WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow))`. Show it from the UI thread: MVUX commands run on a background thread, so call it through a service that dispatches to the window's `DispatcherQueue` (the `uno-mvux` skill, `references/commands.md`). Docs: `features/windows-storage-pickers.md`.

## Choosing and adapting

1. Match the requested behaviour to a contract above. Most real features combine two or three.
2. Compare the recipe's architecture with what discovery found: MVUX or MVVM, navigation style, theme family.
3. Prefer the approach that needs no new dependency and no architectural change. In an existing app, keep its architecture and apply the same contract (see [existing-app.md](existing-app.md)).
4. When a published recipe fits, read its guide, source and tests before copying. Rename namespaces with ordinary edits and build the result. The copied app must not reference this plugin at runtime.
5. Adapting outside a recipe's tested versions or architecture is allowed. Label it as uncertified and verify it locally.

Record the chosen recipe, why it fits and what you adapted in the checkpoint.
