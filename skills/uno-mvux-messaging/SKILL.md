---
name: uno-mvux-messaging
description: "Use messaging to sync MVUX states with entity changes."
when_to_use: "Use when CRUD operations that should update all views showing the same data, decoupling services from models — services broadcast changes, models react, keeping multiple views/pages in sync when shared entities change, or using `EntityMessage<T>` with CommunityToolkit.Mvvm messenger."
metadata:
  author: uno-platform
  version: "2.3"
  category: mvux
---

# MVUX Messaging — Agent Skill

## Workflow

> **Docs lookup:** call `uno_platform_docs_search(...)` first, then `uno_platform_docs_fetch(sourcePath="…")` using the `sourcePath` field from a result (a relative `.md` path; add the result's `anchor` for a section). Never pass a URL, a `.html` link, or a hand-built path.

### Step 1: Fetch the Messaging Documentation

Search for and fetch the messaging documentation:

```
uno_platform_docs_search("MVUX messaging EntityMessage synchronization CRUD")
```

Primary documentation pages:
- **Messaging Reference**: `external/uno.extensions/doc/Learn/Mvux/Advanced/Messaging.md`
- **Messaging How-To**: `external/uno.extensions/doc/Learn/Mvux/Walkthrough/Messaging.howto.md`

Fetch the reference page:

```
uno_platform_docs_fetch(sourcePath="external/uno.extensions/doc/Learn/Mvux/Advanced/Messaging.md")
```

### Step 2: For How-To Walkthroughs

For step-by-step implementation:

```
uno_platform_docs_fetch(sourcePath="external/uno.extensions/doc/Learn/Mvux/Walkthrough/Messaging.howto.md")
```

This covers when to use messaging, sending entity messages from services, observing changes in models, and manual message handling.

### Step 3: Understand the Architecture

From the fetched docs, the messaging pattern involves:
1. **Service** sends `EntityMessage<T>` via `IMessenger.Send(...)`
2. **Model** uses `.Observe(messenger)` on a `ListState` to auto-apply changes
3. **Entity changes** (Created/Updated/Deleted) are applied to the matching state

### Step 4: For Manual Message Handling

If the user needs custom logic when messages arrive, the reference page covers the `Update` extension methods that allow manual application of `EntityMessage<T>` to states.

## Key Principles (Stable)

- Register `IMessenger` as a singleton in DI: `services.AddSingleton<IMessenger, WeakReferenceMessenger>()`
- `EntityMessage<T>` carries an entity change type (Created, Updated, Deleted) and the entity
- `.Observe(messenger)` on a `ListState` auto-applies incoming entity changes
- Records need key equality for matching (see `uno-mvux-records` skill)
- Namespace: `Uno.Extensions.Reactive.Messaging`

## Related Skills

- [[uno-mvux-liststate]] — Mutable collections that messaging updates
- [[uno-mvux-records]] — Key equality for entity matching
- [[uno-mvux-commands]] — Commands that trigger CRUD operations
