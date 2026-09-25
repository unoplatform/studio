---
name: uno-mvux
description: "MVUX (Model-View-Update-eXtended) reactive state management for Uno Platform apps: IFeed<T>, IState<T>, IListFeed<T>, IListState<T>, FeedView, commands, selection, pagination, entity messaging, and immutable partial record Models with generated ViewModels. Use whenever an Uno Platform app loads async data from a service or API, binds user input, shows loading/error/empty states, edits a collection, or needs a Model/ViewModel layer. Also use when the user says MVVM, ViewModel, state management, or data binding in an Uno project, or when the project has <UnoFeatures>MVUX</UnoFeatures> or *Model records. Read this skill before writing any Model, feed, state, or command code, even if the user never says MVUX."
metadata:
  author: uno-platform
  version: "3.0"
  category: mvux
---

# Uno MVUX

MVUX is the state-management pattern Uno Platform projects use instead of hand-written MVVM. A `partial record` Model exposes feeds and states; a source generator produces the bindable ViewModel. Generic agents get MVUX wrong in a handful of predictable ways (inventing an `Update` method, mutating a read-only feed, skipping key equality, using a class instead of a record). This skill exists to stop those mistakes and to route you to the detailed reference for the task at hand.

## Workflow

1. Find the task in the topic map below and read the matching `references/*.md` file before writing code. Each reference is short and self-contained.
2. Ground details in the official docs: call `uno_platform_docs_search(...)`, then `uno_platform_docs_fetch(sourcePath="…")` with the `sourcePath` from a result (a relative `.md` path). Never pass a URL, `.html` link, or hand-built path.
3. Apply the critical rules below. They are the mistakes that compile-fail or silently break UI, so check the generated code against them before finishing.

## Topic map

| Task | Read | Key APIs |
|------|------|----------|
| Explain MVUX, compare with MVVM, add MVUX to a project | `references/overview.md` | `<UnoFeatures>MVUX</UnoFeatures>`, `*Model` → `*ViewModel` |
| Design entities and Models | `references/records.md` | `partial record`, `IKeyEquatable<T>`, `[Key]`, `with` |
| Load a single async value from a service or API | `references/feed-basics.md` | `IFeed<T>`, `Feed.Async`, `Select`, `Where`, `Signal` |
| Load a collection | `references/listfeed.md` | `IListFeed<T>`, `ListFeed.Async`, `IImmutableList<T>` |
| Show a feed in XAML with loading, error, and empty UI | `references/feedview.md` | `mvux:FeedView`, `{Binding Data}`, `Refresh` |
| Two-way bind user input, hold editable state | `references/state-basics.md` | `IState<T>`, `State.Value`, `UpdateAsync`, `ForEach` |
| Add, remove, or edit items in a list | `references/liststate.md` | `IListState<T>`, `AddAsync`, `RemoveAllAsync`, `UpdateAsync` |
| Bind a Button or other control to a Model method | `references/commands.md` | public methods → `IAsyncCommand`, `[ImplicitCommands]` |
| Track the selected item or items of a list | `references/selection.md` | `.Selection(state)`, `IState<T?>`, `IState<IImmutableList<T>>` |
| Infinite scroll or page-by-page loading | `references/pagination.md` | `ListFeed.PaginatedAsync`, `PageRequest` |
| Keep several pages in sync after create/update/delete | `references/messaging.md` | `IMessenger`, `EntityMessage<T>`, `.Observe(messenger)` |

## Critical rules

These come up in nearly every MVUX task. Each reference repeats the ones it needs, but read them once here.

- **Models are `partial record` types with a `Model` suffix** (`MainModel`, `ProductsModel`). The generator emits `MainViewModel`; that generated type is what the page's `DataContext` receives. Services are injected through the record's constructor parameters.
- **Entities are records.** MVUX relies on immutability and value equality. Use `with` expressions to produce modified copies.
- **Feeds are read-only, states are writable.** `IFeed<T>` and `IListFeed<T>` have no `Update`/`Add`/`Remove`; calling them is a compile error. If code must mutate, the property must be `IState<T>` or `IListState<T>` (convert with `ListState.FromFeed(this, feed)`).
- **The mutation method is `UpdateAsync`, and it must be awaited.** There is no `Update` method on `IState<T>`; generic models emit that wrong name constantly. `await state.UpdateAsync(current => ...)`.
- **Updaters are pure.** Derive the new value only from the `current` argument. Do not capture fields or perform side effects inside the lambda; MVUX applies it against a cached value and a non-pure updater gives unstable results. A `static` local function is the safest form.
- **List item types need key equality.** Any record used in `IListFeed<T>` or `IListState<T>` must implement `Uno.Extensions.Equality.IKeyEquatable<T>`, which is auto-generated for a `partial record` with an `Id` or `Key` property, or via `[Key]`. Without it every change re-renders the whole list and `UpdateAsync` cannot find the item. Never invent a substitute interface such as `IHasKey<T>`.
- **`FeedView` binds the feed, templates bind `Data`.** `Source="{Binding MyFeed}"` on the `FeedView`; inside its templates use `{Binding Data.Property}`. `FeedView` already exposes `Refresh`; do not create a refresh command.
- **Any public method on the Model becomes a command.** Method parameters whose name and type match a feed or state receive a snapshot of the current value. Opt out with `[ImplicitCommands(false)]`.
- **Service methods take a `CancellationToken` and return `ValueTask<T>` or `Task<T>`** (`ValueTask<IImmutableList<T>>` for lists).

## Related skills

- `uno-navigation` for moving between pages and passing entities; MVUX Models are registered with `ViewMap`/`DataViewMap` there.
- `uno-toolkit` (`references/itemsrepeater-extensions.md`) when a paginated or selectable list is rendered with `ItemsRepeater` instead of `ListView`.
- `uno-testing` to verify loading, error, and value states of a `FeedView` in a running app.
