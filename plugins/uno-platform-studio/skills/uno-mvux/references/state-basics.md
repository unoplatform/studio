# MVUX State Basics

## Workflow

### Step 1: Fetch the State Reference Documentation

Search for and fetch the state documentation:

```
uno_platform_docs_search("MVUX State IState mutable two-way binding")
```

Primary documentation page:
- **State Reference**: `external/uno.extensions/doc/Reference/Reactive/state.md`

Fetch the full reference:

```
uno_platform_docs_fetch(sourcePath="external/uno.extensions/doc/Reference/Reactive/state.md")
```

### Step 2: Learn State Creation Methods

From the fetched docs, the key factory methods on the `State` and `State<T>` classes:
- `State<T>.Empty(this)` — starts with no value
- `State.Value(this, () => initialValue)` — starts with a sync value (the initial value is a `Func<T>`, not a raw value)
- `State.Async(this, asyncFunc)` — initial value from async source
- `State.FromFeed(this, feed)` — wraps a feed as mutable state

### Step 3: For Updating State Programmatically

The state reference page covers `UpdateAsync`, `SetAsync`, and `ForEach` for subscribing to changes. Focus on the "Update: How to update a state" section.

### Step 4: For Two-Way Binding Examples

The state reference page includes a "Binding the View to a State" section showing how XAML two-way binding works automatically with generated ViewModels.

### Step 5: For Commands with State

If the user needs commands that interact with states:

```
uno_platform_docs_search("MVUX commands state update async method")
```

See also the `references/commands.md`.

## Critical Rules

- **The mutation method is `UpdateAsync`** — `public static ValueTask UpdateAsync<T>(this IState<T> state, Func<T?, T?> updater, CancellationToken ct = default)`.
- **`Update(updater, ct)` is a hidden, deprecated alias, not the API** — it is marked `EditorBrowsable(Never)`, its `CancellationToken` has no default, and its `[Obsolete]` is compiled only into Debug builds of the library, so Release packages accept it silently (the official template still writes `await Count.Update(x => ++x, ct)`). Generic models emit this name constantly; prefer `UpdateAsync`.
- `UpdateAsync` returns `ValueTask` — `await` it from inside an `async` method (typically a command body).
- **The updater must be pure** — derive the new value *solely* from the `current` parameter it receives. Do not capture or read external/mutable variables, and do not perform side effects inside it. MVUX is stateless and lockless: the updater is applied against the state's current cached value, so a function that depends on anything other than `current` is not guaranteed to produce a stable result. The `state.md` example declares its updater as a `static` local function, which the compiler prevents from capturing enclosing state:

  ```csharp
  // Correct: a pure projection of the value you were given
  static int increment(int current) => current + 1;
  await Counter.UpdateAsync(increment);

  // Wrong: result is not derived from `current`, it captures external state
  await Counter.UpdateAsync(_ => _someExternalField);
  ```

## Key Principles (Stable)

- `IState<T>` is **mutable** — it supports read and write operations
- States **cache** their current value (unlike feeds which are stateless)
- The generated ViewModel exposes states as two-way bindable properties automatically
- `State<T>.Empty(this)` requires passing `this` as the owner for lifecycle management
- Use `await state.UpdateAsync(current => newValue)` to update programmatically
- Use `state.ForEach(async (value, ct) => { ... })` to react to changes

## Related Skills

- `references/feed-basics.md` — Read-only reactive data (IFeed<T>)
- `references/commands.md` — Command generation and interaction with states
- `references/liststate.md` — Mutable reactive collections (IListState<T>)
- `references/feedview.md` — Displaying state data with FeedView
