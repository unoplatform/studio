# Generating the Kiota Client

The client is C# that the Kiota CLI generates from an OpenAPI (Swagger) document and that you check in with the app. Never write or edit it by hand: change the document and regenerate. Code from Kiota 1.16 (the template's) and 1.37 both build against the Kiota packages `HttpKiota` brings (Microsoft.Kiota.* 1.22.2 with Uno.Sdk 7.0.0-dev.115).

## Install the CLI

```bash
dotnet tool install --global Microsoft.OpenApi.Kiota
kiota --version
```

`kiota info -l CSharp` then lists packages to install (Microsoft.Kiota.Bundle 2.x). Skip them: the `HttpKiota` feature already references the abstractions, the HTTP library and the JSON, text, form and multipart serializers.

## Generate a new client

Run from the app project folder (the one with the `.csproj`), with the document in `Specs/` as the template does:

```bash
kiota generate -l CSharp -c TodoApiClient -n MyApp.Client.TodoApi \
  -d Specs/TodoApi.json -o Content/Client/TodoApiClient --clean-output
```

| Option | Value |
|---|---|
| `-l` | `CSharp` |
| `-c` | The client class, the type you register with `AddKiotaClient` |
| `-n` | Its namespace; the template uses `<AppName>.Client` and adds `global using <AppName>.Client;` to `GlobalUsings.cs` |
| `-d` | The OpenAPI document: a file path, or a URL such as the running server's `https://localhost:5002/swagger/v1/swagger.json` |
| `-o` | The output folder. The template uses `Content/Client/<Client>`; any folder inside the project compiles |
| `--clean-output` | Deletes files left from an earlier generation, so removed operations disappear |
| `--include-path` / `--exclude-path` | Glob patterns that limit the generated paths, for example `--include-path "/api/todos/**"` |

Prefer a document checked into `Specs/` over a live URL: generation is then repeatable without the server running. The template's `-server` project serves its document at `/swagger/v1/swagger.json` in the Development environment; save it to `Specs/` after an API change.

Generation writes a `kiota-lock.json` next to the client. Check it in: it records the document location (relative to the lock file), the class and namespace names and the Kiota version.

## Regenerate after an API change

```bash
kiota update -o Content/Client/TodoApiClient
```

`kiota update` reads `kiota-lock.json` in that folder and regenerates with the same settings, including the document location; always pass `-o`. A newer CLI upgrades the client and logs `it will be upgraded and you should upgrade dependencies`; the dependencies `HttpKiota` brings still build it, so build the app before reaching for packages.

Then build. A removed or renamed operation now fails at compile time in the code that called it, which is the point: fix those call sites rather than restoring the old file.

## What the generator emits

- `<Client>.cs`: the root, constructed from an `IRequestAdapter`. Its constructor sets the adapter's base URL to the document's first `servers` URL when the adapter has none, which is why a missing `Url` calls that server (`references/register.md`).
- A request builder per path segment (`Api/Todos/TodosRequestBuilder.cs`), with an indexer per path parameter (`Todos[id]`) and a method per operation (`GetAsync`, `PostAsync`, `DeleteAsync`).
- A model per schema under `Models/`: mutable classes with nullable properties and an `AdditionalData` dictionary that collects JSON members the schema does not declare. A schema used by an error response (`4XX`, `404`, `default`) becomes an exception class deriving from `ApiException`.
- Operations whose document has an `operationId` still get method names from the HTTP verb; the `operationId` is not used for C# names.
