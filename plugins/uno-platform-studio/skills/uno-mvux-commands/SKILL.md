---
name: uno-mvux-commands
description: "Create and use commands in MVUX for user interactions."
when_to_use: "Use when binding a Button (or other control) to a method on the Model, understanding how methods become commands in the generated ViewModel, passing feed/state values as parameters to commands, controlling which methods generate commands (implicit vs explicit), or disabling command generation for specific methods."
metadata:
  author: uno-platform
  version: "2.4"
  category: mvux
---

# MVUX Commands — Agent Skill

## Workflow

> **Docs lookup:** call `uno_platform_docs_search(...)` first, then `uno_platform_docs_fetch(sourcePath="…")` using the `sourcePath` field from a result (a relative `.md` path; add the result's `anchor` for a section). Never pass a URL, a `.html` link, or a hand-built path.

### Step 1: Fetch the Commands Documentation

Search for and fetch the commands documentation:

```
uno_platform_docs_search("MVUX commands automatic generation method invocation")
```

Primary documentation pages:
- **Commands Reference**: `external/uno.extensions/doc/Learn/Mvux/Advanced/Commands.md`
- **Commands How-To**: `external/uno.extensions/doc/Learn/Mvux/Walkthrough/Commands.howto.md`

Fetch the reference page for complete details:

```
uno_platform_docs_fetch(sourcePath="external/uno.extensions/doc/Learn/Mvux/Advanced/Commands.md")
```

### Step 2: Understand Implicit Command Generation

From the fetched docs, any public method on a Model automatically becomes a command in the generated ViewModel. The reference page covers:
- Basic command generation rules
- Using `CommandParameter` from XAML
- Additional feed parameters (method parameter names matching feed property names)
- The `[ImplicitCommands]` attribute to enable/disable generation

### Step 3: For How-To Walkthroughs

For step-by-step examples:

```
uno_platform_docs_fetch(sourcePath="external/uno.extensions/doc/Learn/Mvux/Walkthrough/Commands.howto.md")
```

This covers creating basic commands, using feed values in commands, can-execute logic, and disabling command generation.

### Step 4: For Explicit Command Creation

If the user needs manual control over commands, the reference page has an "Explicit command creation" section covering `Command.Create(...)`.

## Critical Rules

- **Feed parameter injection works for any feed type** (`IFeed<T>`, `IListFeed<T>`, `IState<T>`, `IListState<T>`) — the matching parameter receives a *snapshot* of the current value.
- **But you can only mutate writable types from inside a command body.** `IState<T>` and `IListState<T>` expose `UpdateAsync` / `AddAsync` / `RemoveAsync`. `IFeed<T>` and `IListFeed<T>` are **read-only** — calling `.Update(...)`, `.UpdateAsync(...)`, `.AddAsync(...)`, or `.RemoveAsync(...)` on an injected `IListFeed`/`IFeed` is a compile error.
- If a command needs to mutate a collection, the property on the Model must be `IListState<T>` (not `IListFeed<T>`); convert with `ListState.FromFeed(this, sourceFeed)` if needed.

## Key Principles (Stable)

- Any public method on a `*Model` partial record generates a command in the ViewModel
- Commands are automatically `IAsyncCommand` — they handle async properly
- Feed parameter injection: a method parameter whose name/type matches a Feed property gets the current feed value injected
- Use `[ImplicitCommands(false)]` on a Model to disable automatic generation
- Commands are bound in XAML via `Command="{Binding MethodName}"`
- The method name becomes the command name (e.g., `Save()` → `Save` command)

## Related Skills

- [[uno-mvux-state-basics]] — States that commands often update
- [[uno-mvux-feed-basics]] — Feeds whose values commands can consume
- [[uno-mvux-selection]] — Selection-aware commands
