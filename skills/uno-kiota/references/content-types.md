# Non-JSON Content

`AddKiotaClient` builds the client's request adapter with Kiota's JSON parse-node and serialization-writer factories only (unoplatform/uno.extensions#2799). The generated client registers the text, form and multipart factories too, but in Kiota's default registries, which an adapter given explicit factories never consults. Any non-JSON content then throws:

```
System.ArgumentOutOfRangeException: Specified argument was out of the range of valid values. (Parameter 'expected a application/json content type')
```

| Operation | Through `AddKiotaClient` |
|---|---|
| Response `application/json`, request body `application/json` | Works |
| Response `text/plain` (an endpoint returning a bare string) | Throws |
| Request body `application/x-www-form-urlencoded` or `multipart/form-data` (file upload) | Throws before sending |
| Error body `application/problem+json`: ASP.NET Core's `Results.Problem`, `Results.ValidationProblem`, `[ApiController]` validation errors | Throws instead of the mapped error model, so the status code is lost |

Look at the document's `content` entries (and the server's error responses) before choosing a registration. An API that only ever sends and returns `application/json` needs none of this.

## A registration that keeps every serializer

Write the registration `AddKiotaClient` performs, minus the JSON-only factories: Uno's `AddClient<TClient>` with your own factory, an adapter given no factories (so it uses Kiota's default registries, which the generated constructor fills with JSON, text, form and multipart), and Kiota's middleware attached the way `AddKiotaClient` attaches it. Put it in one extension method that the app and its tests both call (`references/testing.md`).

```csharp
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;

public static IServiceCollection AddTodoApi(this IServiceCollection services, HostBuilderContext context, string? url)
{
    const string name = nameof(TodoApiClient);
    var kiotaHandlers = KiotaClientFactory.GetDefaultHandlerActivatableTypes();
    foreach (var handler in kiotaHandlers)
    {
        services.AddTransient(handler);
    }

    return services.AddClient<TodoApiClient>(
        context,
        options: new EndpointOptions { Url = url },
        name: name,
        httpClientFactory: (s, _) =>
        {
            var builder = s
                .AddHttpClient<TodoApiClient>(name)
                .AddTypedClient((httpClient, _) => new TodoApiClient(
                    new HttpClientRequestAdapter(new AnonymousAuthenticationProvider(), httpClient: httpClient)));
            foreach (var handler in kiotaHandlers)
            {
                builder.AddHttpMessageHandler(sp => (DelegatingHandler)sp.GetRequiredService(handler));
            }
            return builder;
        });
}
```

Call it inside `UseHttp`: `services.AddTodoApi(context, context.Configuration["TodoApi:Url"])`. `AddClient` is in the `Uno.Extensions` namespace and is the method `AddKiotaClient` itself calls, so the client keeps the base address from `options`, the native handler, every registered `DelegatingHandler` (the authentication handler included) and, through the loops, Kiota's retry, redirect and user-agent middleware. Without the two loops the 503 retry is gone.

With it, a `text/plain` response returns the string, form and multipart bodies are sent (`MultipartBody` for a file upload), and an `application/problem+json` error throws the mapped error model. Verified by the Uno.Samples `UI/Http.KiotaExtensionsDemo` tests, which also keep one test per content type that fails through plain `AddKiotaClient`, so the day the issue is fixed shows up as a failing test.

Switch the registration only for a client that needs it, and keep `AddKiotaClient` for the rest. When the issue above is fixed, go back to `AddKiotaClient`.

## Changing the API instead

When the API is the developer's own, returning `application/json` everywhere also avoids the problem:

- return a JSON object (`{ "message": "pong" }`) instead of a bare string;
- return errors as `application/json` instead of `application/problem+json`.

Changing the server is the developer's call; ask before doing it.
