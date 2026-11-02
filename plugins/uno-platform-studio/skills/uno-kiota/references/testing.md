# Testing the Client Headlessly

Service tests prove the API wiring without running the app: every operation through the app's own registration, against the real server in-process. They do not replace a runtime check of the UI (the `uno-testing` skill), but they are the evidence for the HTTP layer, and the only runtime evidence when no UI driver is available. Verified by the 21 tests of the Uno.Samples `UI/Http.KiotaExtensionsDemo`.

## Shape

1. **One registration method** that `App.xaml.cs` calls inside `UseHttp` and the tests call too, for example `services.AddTodoApi(context, url)`, registering the client and the app service that wraps it (`references/content-types.md` has its body when the API is not JSON-only; otherwise it wraps `AddKiotaClient`).
2. **The server in-process** with `WebApplicationFactory<Program>` (package `Microsoft.AspNetCore.Mvc.Testing`). A top-level-statements `Program.cs` needs `public partial class Program;` at its end so the test project can name it.
3. **The same handler chain, a different wire.** `UseHttp` registers the native handler as `HttpMessageHandler`, and the client resolves the last registration, so registering the factory's handler after `UseHttp` sends every request into the in-process server while the Uno and Kiota handlers stay in the chain:

```csharp
using Microsoft.AspNetCore.Mvc.Testing;
using Uno.Extensions;

public sealed class TodoApiHost : IDisposable
{
    private readonly WebApplicationFactory<global::Program> _server = new();
    private readonly IHost _host;

    public TodoApiHost() =>
        _host = new Microsoft.Extensions.Hosting.HostBuilder()   // not Uno.Extensions.Hosting.HostBuilder
            .UseHttp((context, services) => services.AddTodoApi(context, "http://localhost"))
            .ConfigureServices(services => services.AddTransient<HttpMessageHandler>(_ => _server.Server.CreateHandler()))
            .Build();

    public IServiceProvider Services => _host.Services;

    public void Dispose()
    {
        _host.Dispose();
        _server.Dispose();
    }
}
```

The test project references the app project (its plain `net10.0` target, which `-tests unit` adds) and the server project. With both `using Uno.Extensions;` and the hosting namespace in scope, a bare `HostBuilder` fails with `CS0104: 'HostBuilder' is an ambiguous reference`; qualify it as above. Create a host per test when the server keeps state, so each test gets a fresh store.

## What to cover

One test per operation the app calls, plus one per behaviour the skill warns about:

- each query parameter, and a path parameter that does not exist (the typed 404);
- the validation error arriving as the generated error model, with its field messages read from the map (`references/calling.md`);
- every non-JSON operation (`text/plain`, form, multipart), and, if the app uses the custom registration, one test per content type proving plain `AddKiotaClient` still throws, so the fix of unoplatform/uno.extensions#2799 shows up as a failing test;
- a 503 with `Retry-After` that succeeds after Kiota's retries, and the same call with `RetryHandlerOption { MaxRetry = 0 }` throwing `ApiException` 503.

Assert on the app service's records, not only on the generated models: the mapping is code too.

## The template's test project fails before any change

On Uno.Sdk 7.0.0-dev.115, the test the `-tests unit` template generates fails as soon as it touches an app type: `System.TypeInitializationException ... Failed to load 'icuuc77'` from the generated `__IcuDataInitializer`. Turn that generator off for the test target only, in the app's project file:

```xml
<PropertyGroup Condition="'$(TargetFramework)' == 'net10.0'">
  <SkipIcuDataInitializerGenerator>true</SkipIcuDataInitializerGenerator>
</PropertyGroup>
<ItemGroup Condition="'$(TargetFramework)' == 'net10.0'">
  <CompilerVisibleProperty Include="SkipIcuDataInitializerGenerator" />
</ItemGroup>
```

The property alone does nothing: the source generator reads it only when it is compiler-visible. The test project's `warning UNOB0025` (runtime-enabled packages without an Uno runtime host) is harmless for service tests.
