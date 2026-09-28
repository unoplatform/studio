# MVUX ListState

## Workflow

> **Docs lookup:** call `uno_platform_docs_search(...)` first, then `uno_platform_docs_fetch(sourcePath="…")` using the `sourcePath` field from a result (a relative `.md` path; add the result's `anchor` for a section). Never pass a URL, a `.html` link, or a hand-built path.

### Step 1: Fetch the ListState Documentation

```
uno_platform_docs_search("MVUX ListState mutable collection add remove update selection")
```

Primary documentation page:
- **ListStates**: `external/uno.extensions/doc/Learn/Mvux/ListStates.md` (creation, `Add`/`Insert`/`Update`/`Remove`/`ForEach` operators, `TrySelectAsync`/`ClearSelection`)

Supporting pages:
- **ListFeed Reference**: `external/uno.extensions/doc/Reference/Reactive/listfeed.md`
- **State Reference**: `external/uno.extensions/doc/Reference/Reactive/state.md`
- **Usage in Apps**: `external/uno.extensions/doc/Reference/Reactive/in-apps.md`

Fetch the ListStates page:

```
uno_platform_docs_fetch(sourcePath="external/uno.extensions/doc/Learn/Mvux/ListStates.md")
```

### Step 2: For ListState Creation

- `ListState<T>.Empty(this)` — starts with no items
- `ListState.Async(this, asyncFunc)` — loads initial data from async source
- Can add `.Selection(...)` to track selected items

### Step 3: For ListState Mutations

Search for mutation operations:

```
uno_platform_docs_search("MVUX ListState add remove update items operation")
```

Key operations: `AddAsync`, `InsertAsync`, `RemoveAllAsync(predicate)`, `UpdateAllAsync(predicate, updater)`, `UpdateItemAsync(item, updater)`, and `UpdateAsync(list => ...)` (the single-delegate overload receives the whole `IImmutableList<T>`).

### Step 4: For Messaging Integration

If the user needs list updates from service CRUD operations:

```
uno_platform_docs_search("MVUX messaging EntityMessage ListState observe")
```

See the `references/messaging.md` for full details.

## Key Principles (Stable)

- `IListState<T>` is **mutable** — supports add/remove/update
- Created with `ListState<T>.Empty(this)` or `ListState.Async(this, ...)`
- Supports two-way binding and selection management
- Records should implement key equality for proper update detection
- Use `.Selection(state)` to connect selection tracking
- For read-only lists, prefer `IListFeed<T>` instead

## Key Equality Requirement (Critical)

**All item types used in `IListState<T>` MUST support key equality** via `Uno.Extensions.Equality.IKeyEquatable<T>`. The `UpdateAsync(T item)` and `UpdateItemAsync(oldItem, updater)` overloads and selection tracking rely on key equality to identify which item to target (`RemoveAllAsync` and `UpdateAllAsync` take a predicate instead). Without it, those overloads do not compile (CS0311, constrained `where T : IKeyEquatable<T>`), and an edited item is diffed as a remove plus an add, which loses selection state.

### Automatic generation (recommended)

For `partial record` types, key equality is **auto-generated** when the record has a property named `Id` or `Key`:

```csharp
public partial record TodoItem(Guid Id, string Title, bool IsComplete);
// IKeyEquatable<TodoItem> is generated automatically — Id is the key
```

### Explicit key configuration

Use `[Key]` attribute when the key property has a different name, or for composite keys:

```csharp
public partial record OrderLine(
    [property: Key] Guid OrderId,
    [property: Key] int LineNumber,
    string Product,
    decimal Price);
```

Either `Uno.Extensions.Equality.KeyAttribute` or `System.ComponentModel.DataAnnotations.KeyAttribute` can be used.

### Assembly-level defaults

Configure which property names are auto-detected as keys:

```csharp
[assembly: ImplicitKeys("Id", "Key", "EntityId")]
```

### Disabling generation

If auto-generation causes issues on a specific type:

```csharp
[ImplicitKeys(IsEnabled = false)]
public partial record MyItem(Guid Id, string Name);
```

### Rules

- The item type **must** be a `partial record` (or manually implement `IKeyEquatable<T>`)
- At least one key property is required — without it, the key-based mutation overloads do not compile and edited items lose selection
- Key properties define **identity** (same entity); non-key properties define **state** (changed data)
- `KeyEquals` returns `true` when two instances represent the same entity, even if other properties differ
- `UpdateAsync(T item)` and `UpdateItemAsync` are constrained `where T : IKeyEquatable<T>` — without key equality they do not compile (CS0311); `UpdateItemAsync` replaces every key-equal item

## Updater Purity (Critical)

The function passed to `UpdateAsync`, `UpdateAllAsync`, or `UpdateItemAsync` **must be pure**: derive the new value *solely* from the `current` value it receives, with no capture of external/mutable variables and no side effects. MVUX is stateless and lockless and applies the updater against the current cached value, so an updater that depends on anything other than its input is not guaranteed to produce a stable result. Project the value you were given onto a new immutable value (use `with` expressions on records); never reach outside the lambda for state.

```csharp
// Correct: pure projection of the matching item
await Items.UpdateAllAsync(item => item.Id == id, item => item with { IsDone = true }, ct);
// or, with the existing instance in hand:
await Items.UpdateItemAsync(existing, item => item with { IsDone = true }, ct);

// Wrong: result captures external mutable state
await Items.UpdateAllAsync(item => item.Id == id, _ => _externalItem, ct);

// Note: Items.UpdateAsync(list => ...) receives the whole IImmutableList<T>, not one item.
```

## Related Skills

- `references/listfeed.md` — Read-only reactive collections
- `references/selection.md` — Selection management
- `references/messaging.md` — Syncing list state with entity changes
- `references/records.md` — Immutable record design with key equality
