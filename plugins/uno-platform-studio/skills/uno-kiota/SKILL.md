---
name: uno-kiota
description: "Typed REST API clients for Uno Platform apps with Uno.Extensions.Http.Kiota: the HttpKiota feature, generating a client from an OpenAPI/Swagger document with the Kiota CLI (kiota generate, kiota update, kiota-lock.json), registering it with AddKiotaClient inside UseHttp, the endpoint Url per environment, request builders, query and path parameters, headers, errors (ApiException, ResponseStatusCode, typed error models), retries and cancellation, and the JSON-only serializer limit (text/plain, form, multipart, application/problem+json). Use whenever an Uno app calls a web API, loads data from a backend or the template's server project, consumes an OpenAPI or Swagger spec, or needs a generated HTTP client, even when the request only says 'get the data from our API'. Also use when the project lists HttpKiota in <UnoFeatures>, has a Content/Client folder or kiota-lock.json, or when an agent is about to write HttpClient calls by hand. Read this skill before writing any API-calling code."
metadata:
  author: uno-platform
  category: http
---

# Uno Kiota

Uno.Extensions.Http.Kiota turns a Kiota-generated API client into a service: `AddKiotaClient<TClient>` gives it an `HttpClient` from the host with the endpoint's base address, the platform's native handler, every `DelegatingHandler` the app registered (the authentication token handler among them) and Kiota's own middleware. The recommended template ships this setup. Generic agents write `HttpClient` calls by hand, hand-edit the generated code, or create the client with `new`; each of those loses the base address, the token and the retry behaviour. This skill routes you to the step.

## Workflow

1. A new app gets Kiota from the `uno-build-app` template command (`-http kiota` is the preset's default); add `-server` for a backend project, an OpenAPI document in `Specs/` and a generated client in `Content/Client`: `dotnet new unoapp -preset recommended -presentation mvux -theme simple -tests unit -server -o <AppName>`. Without `-server` the app has the feature but no client. Then fix the template gaps listed in the critical rules.
2. Read `references/generate.md` to create or regenerate the client, `references/register.md` to register it, and `references/calling.md` to call it. Add `references/content-types.md` when an operation sends or returns anything other than `application/json`, and `references/testing.md` for the service tests that prove every operation.
3. Ground details in the official docs: call `uno_platform_docs_search(...)`, then `uno_platform_docs_fetch(sourcePath="…")` with the `sourcePath` from a result (a relative `.md` path). Never pass a URL, `.html` link, or hand-built path.
4. The API's URL and its OpenAPI document are the developer's. Never invent them: use a placeholder and report the API as not configured.

## Topic map

| Task | Read | Key APIs |
|------|------|----------|
| Install the Kiota CLI, generate a client from a spec file or a running server, regenerate after an API change, limit the generated paths | `references/generate.md` | `kiota generate`, `kiota update`, `kiota-lock.json`, `--include-path` |
| Register the client, its base URL per environment, the configuration section name, default headers and timeout, several APIs | `references/register.md` | `UseHttp`, `AddKiotaClient`, `EndpointOptions`, `configure` |
| Call the API: request builders, path and query parameters, bodies, per-request headers, errors, retries, cancellation, feeding MVUX | `references/calling.md` | `GetAsync`, `QueryParameters`, `ApiException`, `RetryHandlerOption` |
| An operation with `text/plain`, form, multipart or `application/problem+json` content, or the `expected a application/json content type` exception | `references/content-types.md` | `AddClient<TClient>`, `HttpClientRequestAdapter` |
| Service tests of every operation through the app's registration, against the server in-process; the template test's `icuuc77` failure | `references/testing.md` | `WebApplicationFactory`, `UseHttp`, `SkipIcuDataInitializerGenerator` |
| The access token on Kiota calls, refresh on 401, the `No-Refresh` header | the `uno-authentication` skill (`references/http.md`) | `UseAuthentication`, `No-Refresh` |

## Critical rules

- **Feature and namespace:** `HttpKiota` in `<UnoFeatures>`; it brings the Kiota runtime packages, so add none of the packages `kiota info` lists. `AddKiotaClient` is in `Uno.Extensions.Http.Kiota` (the template's `GlobalUsings.cs` has `global using Uno.Extensions.Http.Kiota;`), not in `Uno.Extensions`.
- **Register inside `UseHttp`, inject the client, never `new` it.** `services.AddKiotaClient<TClient>(context, ...)` inside `.UseHttp((context, services) => ...)`, then take `TClient` in the service or Model constructor. A client built from your own `HttpClient` or request adapter gets no base address, no native handler and no token.
- **Generated code is regenerated, not edited.** Change the OpenAPI document, then `kiota update` from the client's folder (`references/generate.md`). Put app logic in a service that wraps the client.
- **Every environment needs the Url.** Without a base address the generated client silently calls the first `servers` URL of the OpenAPI document; the template's is `https://localhost:5002`. The template reads `ApiClient:Url`, which only `appsettings.development.json` sets, so a Release build (Production environment) calls localhost. Set the production Url in `appsettings.json`, or a host the build configuration chooses.
- **Pass the Url as `options`, the way the template does.** In trimmed Release builds (iOS, Android), an endpoint section bound by `AddKiotaClient` loses its `Url` (unoplatform/uno.extensions#3266); `options: new EndpointOptions { Url = context.Configuration["ApiClient:Url"] }` reads the value directly and survives trimming. Passing `options` makes `AddKiotaClient` ignore the section's other keys.
- **Only JSON is parsed or serialized.** `AddKiotaClient` registers only Kiota's JSON factories (unoplatform/uno.extensions#2799). A `text/plain` response, a form or multipart body, and an `application/problem+json` error body (ASP.NET Core's `Results.Problem`, `ValidationProblem`) all throw `ArgumentOutOfRangeException: expected a application/json content type`, which hides the API's status code. `references/content-types.md` has the registration that keeps every serializer.
- **Errors are exceptions.** A status the spec maps to an error schema throws that generated model (it derives from `ApiException`); any other non-success status throws `Microsoft.Kiota.Abstractions.ApiException` with the code in `ResponseStatusCode`. Kiota retries a 503 (also 429 and 504) three more times before throwing, about 21 s without a `Retry-After` header; turn it off per request with `RetryHandlerOption { MaxRetry = 0 }` for calls that must not run twice.
- **Template gaps:** the endpoint Url is missing outside Development (above), `WeatherCache` round-trips the Kiota models through `ISerializer` to get an immutable list (`.ToImmutableList()` is enough), and a `-server` app also gets an unused Refit `Services/Endpoints/IApiClient.cs` and a `<AppName>.DataContracts` project for the weather records. With a Kiota client the OpenAPI document is the shared contract: when you replace the sample API, delete `IApiClient.cs`, `Services/Caching`, the DataContracts project with its references, `global using` lines and `AddJsonTypeInfo(...)` call, and keep `UseSerialization`.

Verified against Uno.Extensions 8.0.0-dev.150 (pinned by Uno.Sdk 7.0.0-dev.115, with Microsoft.Kiota.* 1.22.2), Uno.Templates 7.0.0-dev.120, and clients generated by Kiota 1.16 (the template's) and 1.37. The Uno.Samples `UI/Http.KiotaExtensionsDemo` was built from this skill: a design-first contract (`Specs/TasksApi.yaml`) with every content type above, the registration from `references/content-types.md`, and service tests of every operation. On other versions, check the template output and the docs.

## Related skills

- `uno-authentication` for sign-in and the token the HTTP handler adds to Kiota calls.
- `uno-mvux` for the feeds and states that load data through the client.
- `uno-testing` to exercise the running app against the API.
