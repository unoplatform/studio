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

Register the client through Uno's `AddClient<TClient>` with your own factory, and give the adapter no factories: it then uses Kiota's default registries, which the generated constructor fills with JSON, text, form and multipart.

```csharp
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;

.UseHttp((context, services) =>
{
    services.AddClient<TodoApiClient>(
        context,
        options: new EndpointOptions { Url = context.Configuration["TodoApi:Url"] },
        name: "TodoApi",
        httpClientFactory: (s, _) => s
            .AddHttpClient<TodoApiClient>("TodoApi")
            .AddTypedClient((httpClient, sp) => new TodoApiClient(
                new HttpClientRequestAdapter(new AnonymousAuthenticationProvider(), httpClient: httpClient))));
})
```

`AddClient` is in the `Uno.Extensions` namespace. It is the method `AddKiotaClient` itself calls, so the client keeps the base address from `options`, the native handler, and every registered `DelegatingHandler`, including the authentication handler. With it, a `text/plain` response returns the string, a form body is sent URL-encoded, and an `application/problem+json` error throws the mapped error model.

What it drops is Kiota's middleware (`references/register.md`): no retry on 503, 429 or 504, no Kiota redirect or user-agent handler. Add a `DelegatingHandler` if the app relies on retries.

Verified with a multipart file upload (`MultipartBody`), a form body, a `text/plain` response and an `application/problem+json` 400.

Switch the registration only for a client that needs it, and keep `AddKiotaClient` for the rest. When the issue above is fixed, go back to `AddKiotaClient`.

## Changing the API instead

When the API is the developer's own, returning `application/json` everywhere also avoids the problem:

- return a JSON object (`{ "message": "pong" }`) instead of a bare string;
- return errors as `application/json` instead of `application/problem+json`.

Changing the server is the developer's call; ask before doing it.
