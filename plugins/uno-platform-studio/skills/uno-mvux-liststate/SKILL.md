---
name: uno-mvux-liststate
description: "Create and use IListState<T> for mutable reactive collections in MVUX."
when_to_use: "Use when adding, removing, or updating items in a collection, managing item selection in a list, two-way binding with collections, synchronizing list changes with services via messaging, or converting a read-only `IListFeed<T>` into a mutable `IListState<T>`."
metadata:
  author: uno-platform
  version: "2.3"
  category: mvux
---

# MVUX ListState — Agent Skill

## Workflow

> **Docs lookup:** call `uno_platform_docs_search(...)` first, then `uno_platform_docs_fetch(sourcePath="…")` using the `sourcePath` field from a result (a relative `.md` path; add the result's `anchor` for a section). Never pass a URL, a `.html` link, or a hand-built path.

### Step 1: Fetch the ListState Documentation

There is no dedicated ListState reference page — it shares documentation with ListFeed and State. Search for:

```
uno_platform_docs_search("MVUX ListState mutable collection add remove update selection")
```

Key documentation pages:
- **ListFeed Reference** (includes ListState): `external/uno.extensions/doc/Reference/Reactive/listfeed.md`
- **State Reference** (includes list state creation): `external/uno.extensions/doc/Reference/Reactive/state.md`
- **Usage in Apps** (ListState patterns): `external/uno.extensions/doc/Reference/Reactive/in-apps.md`

Fetch the in-apps reference for practical patterns:

```
uno_platform_docs_fetch(sourcePath="external/uno.extensions/doc/Reference/Reactive/in-apps.md")
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

Key operations: `AddAsync`, `RemoveAllAsync`, `UpdateAsync`, `InsertAsync`.

### Step 4: For Messaging Integration

If the user needs list updates from service CRUD operations:

```
uno_platform_docs_search("MVUX messaging EntityMessage ListState observe")
```

See the `uno-mvux-messaging` skill for full details.

## Key Principles (Stable)

- `IListState<T>` is **mutable** — supports add/remove/update
- Created with `ListState<T>.Empty(this)` or `ListState.Async(this, ...)`
- Supports two-way binding and selection management
- Records should implement key equality for proper update detection
- Use `.Selection(state)` to connect selection tracking
- For read-only lists, prefer `IListFeed<T>` instead

## Key Equality Requirement (Critical)

**All item types used in `IListState<T>` MUST support key equality** via `Uno.Extensions.Equality.IKeyEquatable<T>`. This is essential for `IListState<T>` because mutation operations (`UpdateAsync`, `RemoveAllAsync`, selection tracking) rely on key equality to identify which item to target. Without it, MVUX cannot match an updated instance to its original, causing broken updates, lost selection state, and full list re-renders.

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
[assembly: ImplicitKeyEquality("Id", "Key", "EntityId")]
```

### Disabling generation

If auto-generation causes issues on a specific type:

```csharp
[ImplicitKeys(IsEnabled = false)]
public partial record MyItem(Guid Id, string Name);
```

### Rules

- The item type **must** be a `partial record` (or manually implement `IKeyEquatable<T>`)
- At least one key property is required — without it, mutations and selection cannot identify items
- Key properties define **identity** (same entity); non-key properties define **state** (changed data)
- `KeyEquals` returns `true` when two instances represent the same entity, even if other properties differ
- `UpdateAsync` uses key equality to find the existing item to replace — without it, the update silently fails or replaces the wrong item

## Updater Purity (Critical)

The function passed to `UpdateAsync` (and to the item-level updaters) **must be pure**: derive the new value *solely* from the `current` value it receives, with no capture of external/mutable variables and no side effects. MVUX is stateless and lockless and applies the updater against the current cached value, so an updater that depends on anything other than its input is not guaranteed to produce a stable result. Project the value you were given onto a new immutable value (use `with` expressions on records); never reach outside the lambda for state.

```csharp
// Correct: pure projection of the current item
await Items.UpdateAsync(item => item with { IsDone = true });

// Wrong: result captures external mutable state
await Items.UpdateAsync(_ => _externalItem);
```

## Related Skills

- [[uno-mvux-listfeed]] — Read-only reactive collections
- [[uno-mvux-selection]] — Selection management
- [[uno-mvux-messaging]] — Syncing list state with entity changes
- [[uno-mvux-records]] — Immutable record design with key equality
