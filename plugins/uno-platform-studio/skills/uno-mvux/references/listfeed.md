# MVUX ListFeed

## Workflow

### Step 1: Fetch the ListFeed Reference Documentation

Search for and fetch the documentation:

```
uno_platform_docs_search("MVUX ListFeed reactive collection IListFeed")
```

Primary documentation pages:
- **ListFeed Reference**: `external/uno.extensions/doc/Reference/Reactive/listfeed.md`
- **ListFeed How-To**: `external/uno.extensions/doc/Learn/Mvux/Walkthrough/ListFeed.howto.md`

Fetch the full reference:

```
uno_platform_docs_fetch(sourcePath="external/uno.extensions/doc/Reference/Reactive/listfeed.md")
```

### Step 2: Learn ListFeed Creation Methods

From the fetched docs, the key factory methods on the `ListFeed` static class:
- `ListFeed.Async(...)` — from an `AsyncFunc<IImmutableList<T>>`, i.e. a `ValueTask<IImmutableList<T>>(CancellationToken)` method group; wrap a `Task`-returning method in a lambda
- `ListFeed.AsyncEnumerable(...)` — from a factory `Func<CancellationToken, IAsyncEnumerable<IImmutableList<T>>>`, not an enumerable instance
- `ListFeed.PaginatedAsync(...)` — for paginated/infinite scroll (see `references/pagination.md`)

### Step 3: For ListFeed Operators

The reference page covers `PaginatedAsync`, `Where`, `AsFeed`, and `AsListFeed`. `Where` filters individual items. There is no `Select` on `IListFeed<T>` (`SelectAsync` exists but returns an `IFeed<TResult>`, not a list feed); to project, use `.AsFeed().Select(...)`. `Selection` is documented in `external/uno.extensions/doc/Reference/Reactive/in-apps.md` (anchor `selection`) and `external/uno.extensions/doc/Learn/Mvux/Advanced/Selection.md`.

### Step 4: For How-To Walkthroughs

For step-by-step examples:

```
uno_platform_docs_fetch(sourcePath="external/uno.extensions/doc/Learn/Mvux/Walkthrough/ListFeed.howto.md")
```

This covers loading a list, showing it with FeedView, filtering with `Where`, and when to use feeds vs states.

### Step 5: For Displaying Lists in XAML

The how-to page shows how to bind `IListFeed<T>` to `ListView` via `FeedView` and access items through `{Binding Data}`.

## Key Principles (Stable)

- `IListFeed<T>` is **read-only** — use `IListState<T>` for add/remove/update
- Service methods should return `ValueTask<IImmutableList<T>>` (from `System.Collections.Immutable`)
- `Where` filters individual items; `IListFeed<T>` has no `Select` (`SelectAsync` returns an `IFeed<TResult>`; project via `.AsFeed().Select(...)`)
- ListFeed automatically handles loading/error/empty states
- Use `IListFeed<T>` when data is pulled from a service and is read-only
- Use `IListState<T>` when you need to edit the collection client-side

## Key Equality Requirement (Critical)

**All item types used in `IListFeed<T>` MUST support key equality** via `Uno.Extensions.Equality.IKeyEquatable<T>`. Without key equality, MVUX cannot distinguish between a modified entity and a completely different one: unchanged items still match by value equality, but every edited item is diffed as a remove plus an add instead of an in-place update (lost selection, broken animations).

### Automatic generation (recommended)

For `partial record` types, key equality is **auto-generated** when the record has a property named `Id` or `Key`:

```csharp
public partial record Person(Guid Id, string Name, int Age);
// IKeyEquatable<Person> is generated automatically — Id is the key
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
- At least one key property is required — without it, every edited item is removed and re-added in the UI
- Key properties define **identity** (same entity); non-key properties define **state** (changed data)
- `KeyEquals` returns `true` when two instances represent the same entity, even if other properties differ

## Related Skills

- `references/feed-basics.md` — Single-value feeds
- `references/liststate.md` — Mutable reactive collections
- `references/selection.md` — Selection management with list feeds
- `references/pagination.md` — Infinite scroll / paginated lists
- `references/feedview.md` — Displaying feed data in XAML
