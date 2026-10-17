# MVUX Commands

Any public method on a Model becomes a command in the generated ViewModel, bound with `Command="{Binding MethodName}"`. The rules below are what the generation does with its parameters; `[ImplicitCommands(false)]` on the Model turns generation off, and `Command.Create(...)` builds one by hand (the reference's "Explicit command creation" section).

## Threading

Commands, and the state updates they make, run on a **background thread**. Feeds and states marshal their own changes to the UI, so a Model never needs a dispatcher for them. Anything that must run on the UI thread does need one: a file or folder picker, a `ContentDialog`, a direct change to a control. Put that call in a service that takes the window's `DispatcherQueue` and runs it there when `HasThreadAccess` is false, and inject the service into the Model.

## Critical Rules

- **Feed parameter injection** — a parameter receives a *snapshot* of a feed when its name matches an `IFeed<T>`/`IState<T>` property case-insensitively and its type is exactly `T`; for `IListFeed<T>`/`IListState<T>` the type must be exactly `IImmutableList<T>`. A name match with any other type is not an error: the parameter silently becomes the `CommandParameter`.
- **Eligibility** — a public method becomes a command only with 0 or 1 non-feed parameters and 0 or 1 `CancellationToken`.
- **But you can only mutate writable types from inside a command body.** `IState<T>` exposes `UpdateAsync` for any `T` and `SetAsync` only for struct, nullable-struct and `string` states (for a record state write `await state.UpdateAsync(_ => newValue)`); `IListState<T>` exposes `AddAsync` / `InsertAsync` / `RemoveAllAsync(predicate)` / `UpdateAllAsync` / `UpdateItemAsync` (there is no `RemoveAsync`). `IFeed<T>` and `IListFeed<T>` are **read-only** — calling `.UpdateAsync(...)`, `.AddAsync(...)`, or `.RemoveAllAsync(...)` on an injected `IListFeed`/`IFeed` is a compile error.
- If a command needs to mutate a collection, the property on the Model must be `IListState<T>` (not `IListFeed<T>`); convert with `ListState.FromFeed(this, sourceFeed)` if needed.
- **When mutating via `UpdateAsync`, the updater must be pure** — derive the new value solely from the `current` value, with no captured external state or side effects. See `references/state-basics.md` and `references/liststate.md` for details.

## Key Principles (Stable)

- Any public method on a `*Model` partial record generates a command in the ViewModel
- Commands are automatically `IAsyncCommand` — they handle async properly
- Feed parameter injection: a method parameter whose name (case-insensitive) and exact type match a Feed property gets the current feed value injected
- Use `[ImplicitCommands(false)]` on a Model to disable automatic generation
- Commands are bound in XAML via `Command="{Binding MethodName}"`
- The method name becomes the command name (e.g., `Save()` → `Save` command)

## Docs, for what this page does not cover

Search with `uno_platform_docs_search("MVUX commands …")` and fetch the section by `anchor`:

- **Commands reference**: `external/uno.extensions/doc/Learn/Mvux/Advanced/Commands.md` (generation rules, `CommandParameter`, feed parameters, `[ImplicitCommands]`, explicit command creation)
- **Commands how-to**: `external/uno.extensions/doc/Learn/Mvux/Walkthrough/Commands.howto.md` (basic commands, feed values in commands, can-execute)
- **Threading**: `external/uno.extensions/doc/Reference/Reactive/Architecture.md`, anchor `dispatching-and-threading-considerations`

## Related Skills

- `references/state-basics.md` — States that commands often update
- `references/feed-basics.md` — Feeds whose values commands can consume
- `references/selection.md` — Selection-aware commands
