# Mock Data from the Spec

When the API isn't running yet, or a page needs its loading, empty and error states on demand, serve the generated client from JSON fixture files written from the OpenAPI document. The client, the service that wraps it and the Models stay unchanged: only the last handler in the chain is swapped, in Debug builds only.

**Contents:** [Write the fixtures from the schemas](#write-the-fixtures-from-the-schemas) · [Serve them from a handler](#serve-them-from-a-handler) · [Test the fixtures against the generated models](#test-the-fixtures-against-the-generated-models)

## Write the fixtures from the schemas

One file per response the app reads, named after the operation's `operationId` (`listTodos.json`, `getTodo.json`, `getTodo.404.json` for an error schema), in `Fixtures/` in the app project. The response's schema in the spec decides every field:

- **Only the schema's properties, with its names and types.** `format` decides the text (`date-time` is ISO 8601 with an offset, `uuid` a Guid), an `enum` field takes one of its values, and `required` fields are always present. Use the spec's own `example`s when it has them.
- **Values a user would recognise**, not `string1`: names, titles and amounts that fit the domain, with one at the schema's `maxLength`, one optional field left out, and one non-ASCII name, so the layout is tested too.
- **One set of ids across files.** The record in `getTodo.json` is a row of `listTodos.json`, and a foreign key points to a row that exists.
- **A list holds more than one screen of rows**, and, when the API pages, the paging the client reads (a next link, `@odata.nextLink` and `@odata.count` for OData).
- **A fixture per state the page shows**: an empty list and each error schema the operation declares.

Fixtures don't change: a create doesn't appear in the next list. When the prototype needs that, it is an in-memory service behind the app's own service interface, not more fixtures.

## Serve them from a handler

```csharp
#if DEBUG
public sealed class FixtureHandler : HttpMessageHandler
{
    // One row per operation the app calls, from the spec's paths; the first match wins.
    private static readonly (HttpMethod Method, Regex Path, HttpStatusCode Status, string? File)[] Routes =
    [
        (HttpMethod.Get,    new(@"/api/todos/999$"),          HttpStatusCode.NotFound,  "getTodo.404.json"),
        (HttpMethod.Get,    new(@"/api/todos/[^/?]+$"),       HttpStatusCode.OK,        "getTodo.json"),
        (HttpMethod.Get,    new(@"/api/todos\?.*search=zzz"), HttpStatusCode.OK,        "listTodos.empty.json"),
        (HttpMethod.Get,    new(@"/api/todos(\?|$)"),         HttpStatusCode.OK,        "listTodos.json"),
        (HttpMethod.Delete, new(@"/api/todos/[^/?]+$"),       HttpStatusCode.NoContent, null),
    ];

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        await Task.Delay(400, ct); // long enough to see the loading state
        var path = request.RequestUri!.PathAndQuery;
        foreach (var (method, pattern, status, file) in Routes)
        {
            if (method != request.Method || !pattern.IsMatch(path)) continue;
            var response = new HttpResponseMessage(status) { RequestMessage = request };
            if (file is not null)
            {
                response.Content = new StreamContent(typeof(FixtureHandler).Assembly.GetManifestResourceStream($"Fixtures.{file}")
                    ?? throw new InvalidOperationException($"{file} is not embedded"));
                response.Content.Headers.ContentType = new("application/json");
            }
            return response;
        }
        return new HttpResponseMessage(HttpStatusCode.NotImplemented)
        {
            RequestMessage = request,
            Content = new StringContent($"No fixture for {request.Method} {path}"),
        };
    }
}
#endif
```

Register it inside `UseHttp`, after the client. `UseHttp` registers the native handler before it calls you, and the client takes the last `HttpMessageHandler` registered, so the fixtures replace the wire while every `DelegatingHandler` (`references/register.md`) and Kiota's middleware still run:

```csharp
.UseHttp((context, services) =>
{
    services.AddKiotaClient<TodoApiClient>(context, options: new EndpointOptions { Url = context.Configuration["ApiClient:Url"] });
#if DEBUG
    if (bool.TryParse(context.Configuration["ApiClient:UseFixtures"], out var fixtures) && fixtures)
        services.AddTransient<HttpMessageHandler, FixtureHandler>();
#endif
})
```

```xml
<ItemGroup Condition="'$(Configuration)' == 'Debug'">
  <EmbeddedResource Include="Fixtures\*.json" LogicalName="Fixtures.%(Filename)%(Extension)" />
</ItemGroup>
```

- **Embedded resources load on every head**, WebAssembly and mobile included; the `LogicalName` keeps the resource name independent of the folder and the root namespace.
- **`ApiClient:UseFixtures` goes in `appsettings.development.json`**, so a Release build never sees it, and the handler and files aren't compiled into Release at all.
- **Every client registered through Uno now answers from fixtures**, not only this one. A request with no route gets 501, which Kiota throws as `ApiException` with `ResponseStatusCode` 501 at once, without retrying: add the route, don't widen a pattern.
- **Fixtures are development data.** Report the app as running on fixtures, and the real API as not configured, until a Url replaces them (the `uno-build-app` skill).

## Test the fixtures against the generated models

Kiota never rejects a fixture that drifted from the schema: a misspelled field lands in `AdditionalData`, and a wrong type, an unknown enum value or an unreadable date becomes `null`. Read each file through the generated model, write it back, and fail on every field that didn't survive:

```csharp
public static async Task<string[]> UnreadFields<T>(string json, ParsableFactory<T> factory, bool collection) where T : IParsable
{
    var node = await new JsonParseNodeFactory().GetRootParseNodeAsync("application/json", new MemoryStream(Encoding.UTF8.GetBytes(json)));
    IEnumerable<T> items = collection ? node.GetCollectionOfObjectValues(factory) : [node.GetObjectValue(factory)!];
    var writer = new JsonSerializationWriter();
    if (collection) writer.WriteCollectionOfObjectValues(null, items); else writer.WriteObjectValue(null, items.First());
    var unknown = new List<string>();
    foreach (var item in items) Unknown(item, "$", unknown);
    return Leaves(JsonNode.Parse(json), "$").Except(Leaves(JsonNode.Parse(writer.GetSerializedContent()), "$")).Concat(unknown).ToArray();
}

static void Unknown(object? value, string path, List<string> found)
{
    if (value is not IParsable) return;
    if (value is IAdditionalDataHolder holder) found.AddRange(holder.AdditionalData.Keys.Select(k => $"{path}.{k} (not in the schema)"));
    foreach (var p in value.GetType().GetProperties().Where(p => p.Name != "AdditionalData" && p.GetIndexParameters().Length == 0))
        if (p.GetValue(value) is IParsable child) Unknown(child, $"{path}.{p.Name}", found);
        else if (p.GetValue(value) is IEnumerable list and not string) foreach (var c in list) Unknown(c, $"{path}.{p.Name}[]", found);
}

static IEnumerable<string> Leaves(JsonNode? node, string path) => node switch
{
    JsonObject o => o.SelectMany(kv => Leaves(kv.Value, $"{path}.{kv.Key}")),
    JsonArray a => a.SelectMany((v, i) => Leaves(v, $"{path}[{i}]")),
    null => [],
    _ => [path],
};
```

The namespaces are `Microsoft.Kiota.Abstractions.Serialization`, `Microsoft.Kiota.Serialization.Json`, `System.Collections`, `System.Text` and `System.Text.Json.Nodes`. Use `JsonParseNodeFactory` directly: `KiotaJsonSerializer` throws `Content type application/json does not have a factory registered` until a client has been built. One NUnit case per fixture, reading the embedded file from the app's assembly, asserts that `UnreadFields(json, Todo.CreateFromDiscriminatorValue, collection: true)` is empty; add one case that checks every `Routes` file is embedded and every embedded file has a route. Regenerate the client (`references/generate.md`) and the test shows which fixtures the spec change broke.

This checks names and types, not `required`, `maxLength` or a `pattern`: those are the fixture writer's, from the schema above. Service tests against the real server stay in `references/testing.md`; fixtures prove the app's mapping and its states, not the API.
