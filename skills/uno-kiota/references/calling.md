# Calling the API

The client mirrors the API's paths: one property per path segment, an indexer per path parameter, and a method per HTTP verb. Every method takes an optional request-configuration lambda and a `CancellationToken`.

```csharp
var todos = await client.Api.Todos.GetAsync(cancellationToken: ct);          // GET  /api/todos
var todo  = await client.Api.Todos[id].GetAsync(cancellationToken: ct);      // GET  /api/todos/{id}
var made  = await client.Api.Todos.PostAsync(new Todo { Title = "Milk" }, cancellationToken: ct);
await client.Api.Todos[id].DeleteAsync(cancellationToken: ct);               // 204: returns Task
```

## Query parameters and headers

Both go through the configuration lambda. Query parameter properties are generated from the document; Kiota URL-encodes the values.

```csharp
var page = await client.Api.Todos.GetAsync(config =>
{
    config.QueryParameters.Top = 20;
    config.QueryParameters.Search = text;
    config.Headers.Add("X-Request-Source", "search");  // this request only
}, ct);
```

For a header on every request, use the `configure` callback of `AddKiotaClient` or a `DelegatingHandler` (`references/register.md`). Never add `Authorization` yourself: the authentication handler owns it.

## Results

- Every result is nullable (`List<Todo>?`, `Todo?`); every model property is nullable too. Check before use.
- Models are mutable generated classes. JSON members the schema lacks land in `AdditionalData`, so a stale client silently drops new fields until you regenerate.
- Dates with `format: date-time` arrive as `DateTimeOffset?`.
- A map (`additionalProperties`) has no properties: its entries are in `AdditionalData` as untyped nodes from `Microsoft.Kiota.Abstractions.Serialization`: `UntypedArray` (`GetValue()` returns the items), `UntypedString`, `UntypedInteger`, `UntypedLong`, `UntypedDouble`, `UntypedBoolean`, `UntypedObject`. ASP.NET Core's validation `errors`, field name to messages:

```csharp
var fields = (problem.Errors?.AdditionalData ?? new Dictionary<string, object>()).ToImmutableDictionary(
    e => e.Key,
    e => e.Value is UntypedArray messages
        ? messages.GetValue().OfType<UntypedString>().Select(m => m.GetValue() ?? "").ToImmutableList()
        : ImmutableList<string>.Empty);
```

## Wrapping the client for MVUX

Keep the generated types at the service boundary: map them to the app's records, which MVUX compares by value, and return `ValueTask<IImmutableList<T>>`, the shape `ListFeed.Async` takes (the `uno-mvux` skill, `references/listfeed.md`).

```csharp
public partial record TodoItem(int Id, string Title, bool Done); // partial: Uno generates key equality for Id

public class TodoService(TodoApiClient client) : ITodoService
{
    public async ValueTask<IImmutableList<TodoItem>> GetAll(CancellationToken ct)
    {
        var todos = await client.Api.Todos.GetAsync(cancellationToken: ct);
        return (todos ?? []).Select(t => new TodoItem(t.Id ?? 0, t.Title ?? "", t.Done ?? false)).ToImmutableList();
    }
}
```

```csharp
public IListFeed<TodoItem> Todos => ListFeed.Async(_todoService.GetAll);
```

Register the service in `ConfigureServices` (`services.AddSingleton<ITodoService, TodoService>()`). The template's `WeatherCache` serializes the Kiota models to JSON and back only to get an immutable list; `.ToImmutableList()` does that.

## Errors

| Response | Throws |
|---|---|
| A status the document maps to an error schema (`404`, `4XX`, `default`) | That generated model, which derives from `ApiException`: `catch (ProblemDetails ex)` and read its properties |
| Any other non-success status | `Microsoft.Kiota.Abstractions.ApiException`, `Message` "The server returned an unexpected status code and no error factory is registered for this code: 500" |
| Through `AddKiotaClient`, an error body that is not `application/json`, including `application/problem+json` | `ArgumentOutOfRangeException` (`references/content-types.md`) |
| Cancellation through the token | `TaskCanceledException` |

Every `ApiException` carries `ResponseStatusCode` (an `int`) and `ResponseHeaders`. The generated error model's `Message` is the generic "Exception of type ... was thrown" unless the schema has a property named `message` (Kiota renames it `MessageEscaped` and returns it from `Message`) or marks one with `x-ms-primary-error-message`. Check the generated model before showing `ex.Message` to the user.

```csharp
try
{
    return await client.Api.Todos[id].GetAsync(cancellationToken: ct);
}
catch (ApiException ex) when (ex.ResponseStatusCode == 404)
{
    return null;
}
```

Inside an MVUX feed, let other exceptions through: the feed shows its error state, and `FeedView`'s error template displays it.

A 401 never reaches this code while the user has a session: the authentication handler refreshes and retries first, and clears the session when it cannot (the `uno-authentication` skill, `references/http.md`).

## Retries

Kiota's retry middleware resends a request that got 503 (also 429 and 504) up to three times before the call throws. It waits the `Retry-After` header's delay, or without one 3 s, 6 s and 12 s: a 503 without `Retry-After` surfaces after about 21 seconds, so a loading indicator that seems stuck may be retrying. Turn it off for one request:

```csharp
using Microsoft.Kiota.Http.HttpClientLibrary.Middleware.Options;

await client.Api.Orders.PostAsync(order, config => config.Options.Add(new RetryHandlerOption { MaxRetry = 0 }), ct);
```

Do this for a call that must not run twice, such as a payment, unless the API makes it idempotent.
