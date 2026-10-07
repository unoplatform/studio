# Working with Records in MVUX

## Workflow

### Step 1: Fetch the Records Documentation

```
uno_platform_docs_search("MVUX immutable records data model code generation")
```

Primary documentation pages:
- **Working with Records**: `external/uno.extensions/doc/Learn/Mvux/WorkingWithRecords.md`
- **MVUX Overview** ("Model" and "Creating your own" sections): `external/uno.extensions/doc/Learn/Mvux/Overview.md`

Fetch the records page:

```
uno_platform_docs_fetch(sourcePath="external/uno.extensions/doc/Learn/Mvux/WorkingWithRecords.md")
```

### Step 2: For Key Equality in Messaging/Selection

If the user needs key equality for entity matching (messaging, selection), search for:

```
uno_platform_docs_search("MVUX key equality entity matching record identifier")
```

The messaging reference covers how entity messages are matched by a key selector:
- **Messaging Reference**: `external/uno.extensions/doc/Learn/Mvux/Advanced/Messaging.md`

### Step 3: For General C# Records

If the user needs help with C# record syntax itself, this is standard C# language knowledge — records provide immutability, value equality, `with` expressions, and deconstruction.

## Key Equality Requirement (Critical)

**Record types used as items in MVUX collections (`IListFeed<T>`, `IListState<T>`) MUST support key equality via `Uno.Extensions.Equality.IKeyEquatable<T>`.** Without it, MVUX cannot distinguish a modified entity from a different one — unchanged items still match by value equality, but every edited item becomes a remove plus an add (lost selection, broken animations), and `UpdateAsync(T item)`/`UpdateItemAsync` do not compile (CS0311, constrained `where T : IKeyEquatable<T>`). Messaging (`Observe`) matches by its own key selector, not by `IKeyEquatable<T>`.

### Automatic generation (recommended)

For `partial record` types, `IKeyEquatable<T>` is **auto-generated** when the record has a property named `Id` or `Key`:

```csharp
public partial record Person(Guid Id, string Name, int Age);
// IKeyEquatable<Person> is generated automatically — Id is the key
```

### Explicit key configuration

Use `[Key]` (from `Uno.Extensions.Equality` or `System.ComponentModel.DataAnnotations`) when the key property has a different name, or for composite keys:

```csharp
public partial record OrderLine(
    [property: Key] Guid OrderId,
    [property: Key] int LineNumber,
    string Product,
    decimal Price);
```

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

### Do not invent the interface name

The interface is exactly **`Uno.Extensions.Equality.IKeyEquatable<T>`**. Do not introduce a generic `IHasKey<T>` or similar invented name — the framework will not recognise it.

### Rules

- The item type **must** be a `partial record` (or manually implement `IKeyEquatable<T>`); a non-partial record with an `Id`/`Key` property is build error KE0001
- At least one key property is required — without it, every edited item is removed and re-added in the UI
- Key properties define **identity** (same entity); non-key properties define **state** (changed data)
- `KeyEquals` returns `true` when two instances represent the same entity, even if other properties differ

## Key Principles (Stable)

- **Data entities** should be `record` types (immutable, value equality)
- **Models** are `partial` types with a `Model` suffix; `partial record` is the convention, a `partial class` also generates
- Records use `with` expressions to create modified copies: `entity with { Name = "New" }`
- Value equality means two records with the same data are considered equal
- Positional records: `public record Person(string Name, int Age);` — concise syntax
- Models inject services via constructor: `public partial record MainModel(IMyService Service)`

## Related Skills

- `references/overview.md` — MVUX architecture requiring records
- `references/messaging.md` — Entity messages matched by key selector
- `references/selection.md` — Selection tracking relies on equality
