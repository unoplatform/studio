---
name: uno-mvux-feed-basics
description: "Create and use IFeed<T> for async data in MVUX."
when_to_use: "Use when loading data from a service or API into MVUX, creating reactive data sources that automatically handle loading/error states, transforming async data with operators like `Select` or `Where`, understanding the difference between feeds and states, or refreshing data using `Signal`."
metadata:
  author: uno-platform
  version: "2.3"
  category: mvux
---

# MVUX Feed Basics — Agent Skill

## Workflow

> **Docs lookup:** call `uno_platform_docs_search(...)` first, then `uno_platform_docs_fetch(sourcePath="…")` using the `sourcePath` field from a result (a relative `.md` path; add the result's `anchor` for a section). Never pass a URL, a `.html` link, or a hand-built path.

### Step 1: Fetch the Feed Reference Documentation

Search for and fetch the feed documentation:

```
uno_platform_docs_search("MVUX Feed IFeed async data creation")
```

Primary documentation pages:
- **Feed Reference**: `external/uno.extensions/doc/Reference/Reactive/feed.md`
- **Simple Feed How-To**: `external/uno.extensions/doc/Learn/Mvux/Walkthrough/SimpleFeed.howto.md`

Fetch the reference page for complete API details:

```
uno_platform_docs_fetch(sourcePath="external/uno.extensions/doc/Reference/Reactive/feed.md")
```

### Step 2: Learn Feed Creation Methods

From the fetched docs, the key factory methods on the `Feed` static class are:
- `Feed.Async(...)` — from a `Task<T>` returning method
- `Feed.AsyncEnumerable(...)` — from an `IAsyncEnumerable<T>`
- `Feed.Create(...)` — from a custom async function

### Step 3: For Feed Operators (Select, Where)

If the user needs to transform feed data, search for operators:

```
uno_platform_docs_search("MVUX feed operators Select Where transform")
```

The feed reference page covers all operators in the "Operators" section.

### Step 4: For Feed Refresh / Signal

If the user needs to trigger a feed refresh:

```
uno_platform_docs_search("MVUX feed refresh Signal trigger reload")
```

The feed reference page has a section on `Signal` for manual refresh triggers.

### Step 5: For How-To Walkthroughs

For step-by-step examples of showing feed data on the UI:

```
uno_platform_docs_fetch(sourcePath="external/uno.extensions/doc/Learn/Mvux/Walkthrough/SimpleFeed.howto.md")
```

This covers binding feed data, showing errors, and using feeds inside DataTemplates.

## Key Principles (Stable)

- `IFeed<T>` is **read-only** — it cannot be updated from the UI
- Feeds automatically track loading/error/none/value states
- Feeds are **stateless** — they don't cache values (use `IState<T>` for caching)
- The service method should accept a `CancellationToken` parameter for proper cancellation
- Service return types should use `ValueTask<T>` or `Task<T>`
- For collections, use `IListFeed<T>` instead (see `uno-mvux-listfeed` skill)

## Related Skills

- [[uno-mvux-overview]] — MVUX architecture concepts
- [[uno-mvux-feedview]] — Displaying feed data with FeedView control
- [[uno-mvux-state-basics]] — Mutable reactive data (IState<T>)
- [[uno-mvux-listfeed]] — Reactive collections (IListFeed<T>)
