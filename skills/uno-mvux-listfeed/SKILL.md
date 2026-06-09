---
name: uno-mvux-listfeed
description: "Create and use IListFeed<T> for reactive collections in MVUX."
when_to_use: "Use when loading a list of items from a service or API, displaying collections in `ListView`, `GridView`, or `ItemsRepeater`, filtering list data reactively, or choosing between `IListFeed<T>` (read-only) and `IListState<T>` (mutable)."
metadata:
  author: uno-platform
  version: "2.3"
  category: mvux
---

# MVUX ListFeed — Agent Skill

## Workflow

> **Docs lookup:** call `uno_platform_docs_search(...)` first, then `uno_platform_docs_fetch(sourcePath="…")` using the `sourcePath` field from a result (a relative `.md` path; add the result's `anchor` for a section). Never pass a URL, a `.html` link, or a hand-built path.

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
- `ListFeed.Async(...)` — from a method returning `Task<IImmutableList<T>>`
- `ListFeed.AsyncEnumerable(...)` — from an `IAsyncEnumerable<IImmutableList<T>>`
- `ListFeed.PaginatedAsync(...)` — for paginated/infinite scroll (see `uno-mvux-pagination` skill)

### Step 3: For ListFeed Operators

The reference page covers operators like `Where`, `Select`, and `AsFeed`. These operate on individual items in the collection.

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
- Operators like `Where` and `Select` work on individual items, not the list itself
- ListFeed automatically handles loading/error/empty states
- Use `IListFeed<T>` when data is pulled from a service and is read-only
- Use `IListState<T>` when you need to edit the collection client-side

## Key Equality Requirement (Critical)

**All item types used in `IListFeed<T>` MUST support key equality** via `Uno.Extensions.Equality.IKeyEquatable<T>`. Without key equality, MVUX cannot distinguish between a modified entity and a completely different one, causing full list re-renders instead of granular item updates (flickering, lost scroll position, broken animations).

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
- At least one key property is required — without it, every update replaces the entire list in the UI
- Key properties define **identity** (same entity); non-key properties define **state** (changed data)
- `KeyEquals` returns `true` when two instances represent the same entity, even if other properties differ

## Related Skills

- [[uno-mvux-feed-basics]] — Single-value feeds
- [[uno-mvux-liststate]] — Mutable reactive collections
- [[uno-mvux-selection]] — Selection management with list feeds
- [[uno-mvux-pagination]] — Infinite scroll / paginated lists
- [[uno-mvux-feedview]] — Displaying feed data in XAML
