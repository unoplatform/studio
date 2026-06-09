---
name: uno-mvux-state-basics
description: "Create and use IState<T> for mutable reactive data in MVUX."
when_to_use: "Use when implementing two-way data binding (e.g., TextBox, Slider, ToggleSwitch), accepting and storing user input, maintaining editable application state, programmatically updating a value from a command or service call, or understanding the difference between `IFeed<T>` (read-only) and `IState<T>` (read/write)."
metadata:
  author: uno-platform
  version: "2.4"
  category: mvux
---

# MVUX State Basics — Agent Skill

## Workflow

> **Docs lookup:** call `uno_platform_docs_search(...)` first, then `uno_platform_docs_fetch(sourcePath="…")` using the `sourcePath` field from a result (a relative `.md` path; add the result's `anchor` for a section). Never pass a URL, a `.html` link, or a hand-built path.

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
- `State<T>.Value(this, initialValue)` — starts with a sync value
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

See also the `uno-mvux-commands` skill.

## Critical Rules

- **The mutation method is `UpdateAsync`** — `public static ValueTask UpdateAsync<T>(this IState<T> state, Func<T?, T?> updater, CancellationToken ct = default)`.
- **There is no `Update` method on `IState<T>`** — calling `state.Update(x => ...)` is a compile error. The baseline model frequently emits this wrong name; the skill must keep the correct name front-and-centre.
- `UpdateAsync` returns `ValueTask` — `await` it from inside an `async` method (typically a command body).
- **The updater must be pure** — derive the new value *solely* from the `current` parameter it receives. Do not capture or read external/mutable variables, and do not perform side effects inside it. MVUX is stateless and lockless: the updater is applied against the state's current cached value, so a function that depends on anything other than `current` is not guaranteed to produce a stable result. The official docs model this by declaring the updater as a `static` local function, which the compiler prevents from capturing enclosing state:

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

- [[uno-mvux-feed-basics]] — Read-only reactive data (IFeed<T>)
- [[uno-mvux-commands]] — Command generation and interaction with states
- [[uno-mvux-liststate]] — Mutable reactive collections (IListState<T>)
- [[uno-mvux-feedview]] — Displaying state data with FeedView
