---
name: uno-mvux-overview
description: "Understand MVUX (Model-View-Update-eXtended) architecture in Uno Platform."
when_to_use: "Use when learning the MVUX architecture, comparing MVUX to MVVM or other patterns, setting up a new Uno Platform project with MVUX, or understanding immutable data flow with feeds and states."
metadata:
  author: uno-platform
  version: "2.5"
  category: mvux
---

# MVUX Overview — Agent Skill

## Workflow

> **Docs lookup:** call `uno_platform_docs_search(...)` first, then `uno_platform_docs_fetch(sourcePath="…")` using the `sourcePath` field from a result (a relative `.md` path; add the result's `anchor` for a section). Never pass a URL, a `.html` link, or a hand-built path.

### Step 1: Fetch the MVUX Overview Documentation

Search for and fetch the core MVUX overview page:

```
uno_platform_docs_search("MVUX overview Model View Update eXtended architecture")
```

The primary documentation page is:
- **MVUX Overview**: `external/uno.extensions/doc/Learn/Mvux/Overview.md`

Fetch the full page for comprehensive details:

```
uno_platform_docs_fetch(sourcePath="external/uno.extensions/doc/Learn/Mvux/Overview.md")
```

### Step 2: Understand Key Concepts

From the fetched documentation, extract and explain:

1. **What MVUX is**: Model-View-Update-eXtended — an MVU variant that supports XAML data binding
2. **How it differs from MVVM**: Unidirectional data flow, immutable records, automatic state management
3. **Core types**: `IFeed<T>`, `IState<T>`, `IListFeed<T>`, `IListState<T>`
4. **Code generation**: Models with `*Model` suffix generate corresponding `*ViewModel` classes
5. **FeedView control**: The UI control that handles loading/error/value states automatically

### Step 3: For Project Setup Details

If the user needs to set up MVUX in a project, search for setup instructions:

```
uno_platform_docs_search("MVUX project setup UnoFeatures add MVUX existing app")
```

Key doc page for setup:
- **How to set up an MVUX project**: `external/uno.extensions/doc/Learn/Mvux/Tutorials/HowTo-MvuxProject.md`

### Step 4: For MVUX vs MVVM Comparison

If the user wants a comparison, the overview page contains a detailed MVVM vs MVUX data flow section. Fetch:

```
uno_platform_docs_fetch(sourcePath="external/uno.extensions/doc/Learn/Mvux/Overview.md")
```

Focus on the sections: "What is MVUX?", "eXtended", "MVVM vs MVUX Data Flow", and "Weather App Example".

## Key Principles (Stable)

- Models MUST be `partial record` types with a `Model` suffix (e.g., `MainModel`)
- All data entities should be C# records (immutable)
- MVUX requires `<UnoFeatures>MVUX</UnoFeatures>` in the project file
- The generated ViewModel is what gets set as the `DataContext`, not the Model itself
- Services are injected via constructor parameters on the record

## Choosing the right MVUX skill

When unsure which MVUX skill applies, pick by intent:

### Getting started

- [[uno-mvux-records]] — Design immutable data models
- [[uno-mvux-feed-basics]] — Load async data

### Displaying data

- [[uno-mvux-feedview]] — `FeedView` control for all states
- [[uno-mvux-listfeed]] — Display collections

### User input

- [[uno-mvux-state-basics]] — Two-way binding with states
- [[uno-mvux-commands]] — Button actions and commands

### Advanced features

- [[uno-mvux-liststate]] — Mutable collections
- [[uno-mvux-selection]] — Selection management
- [[uno-mvux-pagination]] — Infinite scroll
- [[uno-mvux-messaging]] — Entity synchronization
