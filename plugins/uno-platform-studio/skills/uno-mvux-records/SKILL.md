---
name: uno-mvux-records
description: "Use immutable records effectively with MVUX."
when_to_use: "Use when designing data models/entities for MVUX, understanding why MVUX requires records (immutability), implementing key equality for proper item matching in lists, using `with` expressions to create modified copies, or understanding `partial record` requirements for Model classes."
metadata:
  author: uno-platform
  version: "2.4"
  category: mvux
---

# Working with Records in MVUX — Agent Skill

## Workflow

> **Docs lookup:** call `uno_platform_docs_search(...)` first, then `uno_platform_docs_fetch(sourcePath="…")` using the `sourcePath` field from a result (a relative `.md` path; add the result's `anchor` for a section). Never pass a URL, a `.html` link, or a hand-built path.

### Step 1: Fetch the MVUX Overview (Records Section)

The MVUX overview covers why records are essential:

```
uno_platform_docs_search("MVUX immutable records data model code generation")
```

Primary documentation page:
- **MVUX Overview**: `external/uno.extensions/doc/Learn/Mvux/Overview.md`

Fetch and look for the "Model" and "Creating your own" sections:

```
uno_platform_docs_fetch(sourcePath="external/uno.extensions/doc/Learn/Mvux/Overview.md")
```

### Step 2: For Key Equality in Messaging/Selection

If the user needs key equality for entity matching (messaging, selection), search for:

```
uno_platform_docs_search("MVUX key equality entity matching record identifier")
```

The messaging reference covers how records are matched by key:
- **Messaging Reference**: `external/uno.extensions/doc/Learn/Mvux/Advanced/Messaging.md`

### Step 3: For General C# Records

If the user needs help with C# record syntax itself, this is standard C# language knowledge — records provide immutability, value equality, `with` expressions, and deconstruction.

## Key Equality Requirement (Critical)

**Record types used as items in MVUX collections (`IListFeed<T>`, `IListState<T>`) MUST support key equality via `Uno.Extensions.Equality.IKeyEquatable<T>`.** Without it, MVUX cannot distinguish a modified entity from a different one — every change forces a full list re-render (flickering, lost scroll position, broken animations). For single-value `IState<T>` / `IFeed<T>`, key equality matters for messaging-driven entity matching.

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
[assembly: ImplicitKeyEquality("Id", "Key", "EntityId")]
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

- The item type **must** be a `partial record` (or manually implement `IKeyEquatable<T>`)
- At least one key property is required — without it, every update replaces the entire list in the UI
- Key properties define **identity** (same entity); non-key properties define **state** (changed data)
- `KeyEquals` returns `true` when two instances represent the same entity, even if other properties differ

## Key Principles (Stable)

- **Data entities** should be `record` types (immutable, value equality)
- **Model classes** must be `partial record` with a `Model` suffix for code generation
- Records use `with` expressions to create modified copies: `entity with { Name = "New" }`
- Value equality means two records with the same data are considered equal
- Positional records: `public record Person(string Name, int Age);` — concise syntax
- Models inject services via constructor: `public partial record MainModel(IMyService Service)`

## Related Skills

- [[uno-mvux-overview]] — MVUX architecture requiring records
- [[uno-mvux-messaging]] — Entity matching depends on key equality
- [[uno-mvux-selection]] — Selection tracking relies on equality
