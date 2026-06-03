---
name: uno-mvux-feedview
description: "Display async feed data with FeedView control."
when_to_use: "Use when displaying data from an `IFeed<T>` or `IState<T>` in XAML, showing loading spinners, error messages, or empty-state UI automatically, customizing templates for different data states (value, progress, error, none), or binding feed data inside a `DataTemplate`."
metadata:
  author: uno-platform
  version: "2.3"
  category: mvux
---

# FeedView Control — Agent Skill

## Workflow

> **Docs lookup:** call `uno_platform_docs_search(...)` first, then `uno_platform_docs_fetch(sourcePath="…")` using the `sourcePath` field from a result (a relative `.md` path; add the result's `anchor` for a section). Never pass a URL, a `.html` link, or a hand-built path.

### Step 1: Fetch the FeedView Documentation

Search for and fetch the FeedView documentation:

```
uno_platform_docs_search("MVUX FeedView control displaying async data templates")
```

Primary documentation pages:
- **FeedView Reference**: `external/uno.extensions/doc/Learn/Mvux/FeedView.md`
- **FeedView How-To**: `external/uno.extensions/doc/Learn/Mvux/Walkthrough/FeedView.howto.md`

Fetch the full reference page:

```
uno_platform_docs_fetch(sourcePath="external/uno.extensions/doc/Learn/Mvux/FeedView.md")
```

### Step 2: Understand FeedView Templates

From the fetched docs, extract the available templates:
- **ValueTemplate** (default `<DataTemplate>`) — shown when data is available
- **ProgressTemplate** — shown during loading
- **ErrorTemplate** — shown when an error occurs
- **NoneTemplate** — shown when there is no data

### Step 3: For FeedView How-To Walkthroughs

For step-by-step implementation guidance:

```
uno_platform_docs_fetch(sourcePath="external/uno.extensions/doc/Learn/Mvux/Walkthrough/FeedView.howto.md")
```

This covers rendering `IFeed<T>`, `IState<T>`, customizing templates, and refresh behavior.

### Step 4: For FeedView with Lists

If the user is displaying a collection feed, search for:

```
uno_platform_docs_search("MVUX FeedView list feed ListView collection display")
```

Key page:
- **ListFeed How-To**: `external/uno.extensions/doc/Learn/Mvux/Walkthrough/ListFeed.howto.md`

## Key Principles (Stable)

- XAML namespace: `xmlns:mvux="using:Uno.Extensions.Reactive.UI"`
- `Source` property binds to the feed/state: `Source="{Binding MyFeed}"`
- Inside templates, access the data via `{Binding Data}` (e.g., `{Binding Data.Name}`)
- The default `<DataTemplate>` content child acts as the `ValueTemplate`
- FeedView automatically provides a `Refresh` command — no need to create one manually
- FeedView handles all state transitions (loading → value → error) without code-behind

## Related Skills

- [[uno-mvux-feed-basics]] — Creating feeds
- [[uno-mvux-state-basics]] — Creating mutable states
- [[uno-mvux-listfeed]] — Reactive collections
