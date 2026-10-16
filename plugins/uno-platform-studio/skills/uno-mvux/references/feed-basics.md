# MVUX Feed Basics

## Create a feed

The `Feed` static class has three factories:

- `Feed.Async(...)` — from an `AsyncFunc<T>` (`ValueTask<T>(CancellationToken)`); a `ValueTask<T>` method group passes directly, a `Task<T>` method needs `async ct => await ...`
- `Feed.AsyncEnumerable(...)` — from a factory `Func<CancellationToken, IAsyncEnumerable<T>>`, not an enumerable instance
- `Feed.Create(...)` — from a factory returning an `IAsyncEnumerable<Message<T>>` stream (low level; rarely needed)

## Refresh a feed

- **From the Model**, for example after a save changes what the feed loads: pass a `Signal` as the `refresh:` argument and raise it.

  ```csharp
  private readonly Signal _changed = new();

  public IListFeed<Asset> Assets => ListFeed.Async(_store.GetAssetsAsync, _changed);

  public async ValueTask Save(CancellationToken ct)
  {
      await _store.SaveAsync(ct);
      _changed.Raise();
  }
  ```

  `Feed.Async`, `ListFeed.Async` and `State.Async` all take it. Several feeds that read the same store can share one `Signal`, which the store can expose so every Model that reads it refreshes after any write.
- **From the view**: the `FeedView`'s `Refresh` command, for example a button in its template bound to `{Binding Refresh}` (see `references/feedview.md`).

## Key Principles (Stable)

- `IFeed<T>` is **read-only** — it cannot be updated from the UI
- Feeds automatically track loading/error/none/value states
- Feeds are **stateless** — they don't cache values (use `IState<T>` for caching)
- The service method should accept a `CancellationToken` parameter for proper cancellation
- Service return types should use `ValueTask<T>` (a `Task<T>` method must be wrapped in a lambda for `Feed.Async`)
- For collections, use `IListFeed<T>` instead (see `references/listfeed.md`)

## Docs, for what this page does not cover

Search with `uno_platform_docs_search("MVUX feed operators Select Where")` (or your question) and fetch the section by `anchor`. The pages:

- **Feed reference**: `external/uno.extensions/doc/Reference/Reactive/feed.md` (factories, operators such as `Select` and `Where`; it has no `Signal` section)
- **Refreshing from the view**: `external/uno.extensions/doc/Reference/Reactive/in-apps.md`, anchor `refreshing-a-data`
- **Simple feed how-to**: `external/uno.extensions/doc/Learn/Mvux/Walkthrough/SimpleFeed.howto.md` (binding feed data, errors, feeds inside DataTemplates)

## Related Skills

- `references/overview.md` — MVUX architecture concepts
- `references/feedview.md` — Displaying feed data with FeedView control
- `references/state-basics.md` — Mutable reactive data (IState<T>)
- `references/listfeed.md` — Reactive collections (IListFeed<T>)
