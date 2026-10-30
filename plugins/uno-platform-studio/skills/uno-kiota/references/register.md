# Registering the Kiota Client

`AddKiotaClient<TClient>` registers the generated client as a typed `HttpClient` service. Register it inside `UseHttp` in `App.xaml.cs` and inject `TClient` where it is needed.

```csharp
.UseHttp((context, services) =>
{
    services.AddKiotaClient<TodoApiClient>(
        context,
        options: new EndpointOptions { Url = context.Configuration["TodoApi:Url"] });
})
```

`UseHttp` registers the native handler, the cookie manager and Uno's `DiagnosticHandler`; keep it even when the client is the only registration. The namespace is `Uno.Extensions.Http.Kiota`.

## What the registered client gets

From the outside in, a request passes through:

1. Kiota's middleware: URI replacement, retry (`references/calling.md`), redirect, parameter-name decoding, the `User-Agent: kiota-dotnet/<version>` header, header and body inspection.
2. Every service registered as `DelegatingHandler`, chained in registration order: `UseHttp`'s `DiagnosticHandler`, the template's Debug-only `DebugHttpHandler`, the authentication handler from `UseAuthentication`, and any of your own (`services.AddTransient<DelegatingHandler, MyHandler>()`).
3. The platform's native `HttpMessageHandler`, unless `UseNativeHandler` is `false`.

The client is created with Kiota's anonymous authentication provider: the token comes from the authentication handler in step 2, so never configure a Kiota `IAuthenticationProvider` (the `uno-authentication` skill, `references/http.md`).

## The base URL

`EndpointOptions.Url` becomes the `HttpClient.BaseAddress`, which Kiota uses as its base URL. A trailing slash and a base path both work: `https://host/v1` and `https://host/v1/` both call `https://host/v1/api/todos`.

When no Url reaches the client, the generated constructor falls back to the OpenAPI document's first `servers` URL, with no error. Every environment the app ships in must set it:

| File | Loaded |
|---|---|
| `appsettings.json` | Always. The template sets no `ApiClient:Url` here, so a Release build (Production) falls back to `https://localhost:5002` |
| `appsettings.development.json` | Debug builds only: the template calls `UseEnvironment(Environments.Development)` under `#if DEBUG` |

The address can also differ per head, so compute it once and pass it as `options`:

| Head | Development address |
|---|---|
| Desktop, Windows, iOS simulator | The configured `http://localhost:<port>`. iOS allows plain HTTP to it only with `NSAppTransportSecurity` / `NSAllowsLocalNetworking` in `Info.plist` |
| WebAssembly served by the `-server` project | The page's own origin, `JSHost.GlobalThis.GetPropertyAsJSObject("location")!.GetPropertyAsString("origin")` under `OperatingSystem.IsBrowser()`; any other host is cross-origin and needs CORS on the server |
| Android emulator | `localhost` is the emulator itself; the development machine is `10.0.2.2`. Plain HTTP also needs a `network_security_config.xml` that permits cleartext to that host |

The template's development address is `https://localhost:5002` with `UseHttpsRedirection()`, which needs a trusted development certificate on every head; a sample or prototype is simpler on plain `http://localhost:<port>` in Development.

## Options or a configuration section

| Registration | Url comes from | Other section keys |
|---|---|---|
| `options: new EndpointOptions { Url = context.Configuration["TodoApi:Url"] }` (template) | The value you pass | Ignored, including `UseNativeHandler` (it defaults to `true`) |
| `name: "TodoApi"`, no `options` | The `TodoApi` section: `"TodoApi": { "Url": "...", "UseNativeHandler": true }` | Bound |
| Neither | The section named after the client's **full** type name, `MyApp.Client.TodoApiClient`; a `TodoApiClient` section is not read | Bound |

Prefer the template's form: a section bound by `AddKiotaClient` loses its `Url` in trimmed Release builds (iOS, Android), where the trimmer removes the `EndpointOptions` setters that only binding reaches (unoplatform/uno.extensions#3266). The client then calls the `servers` URL. An `options` object with a `null` `Url` does not fall back to the section either.

For extra per-endpoint settings, subclass `EndpointOptions` and use `AddKiotaClientWithEndpoint<TClient, TEndpoint>`; the same trimming caveat applies to its binding.

## Default headers, timeout, handler

The `configure` callback receives the `IHttpClientBuilder` and the options:

```csharp
services.AddKiotaClient<TodoApiClient>(
    context,
    options: new EndpointOptions { Url = context.Configuration["TodoApi:Url"] },
    configure: (builder, endpoint) => builder.ConfigureHttpClient(client =>
    {
        client.DefaultRequestHeaders.Add("X-Api-Key", context.Configuration["TodoApi:ApiKey"]);
        client.Timeout = TimeSpan.FromSeconds(30);
    }));
```

An API key in `appsettings.json` ships inside the app package and is readable by anyone who has it; treat it as public. A user's credential belongs in `uno-authentication`.

## Several APIs

Register one client per API, each with its own Url:

```csharp
services.AddKiotaClient<TodoApiClient>(context, options: new EndpointOptions { Url = context.Configuration["TodoApi:Url"] });
services.AddKiotaClient<WeatherApiClient>(context, options: new EndpointOptions { Url = context.Configuration["WeatherApi:Url"] });
```

Each client is a distinct type, so the registrations never collide. Every `DelegatingHandler` applies to every client registered through Uno (`AddKiotaClient`, `AddClient`, `AddRefitClient`): with `UseAuthentication`, the user's token goes to the weather API too. When a third-party API must not receive it, tell the developer before registering it; a client built with plain `services.AddHttpClient<TClient>()` gets none of Uno's handlers, but none of its base address or native handler either.
