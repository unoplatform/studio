---
type: llm
---
The reply diagnoses an Uno Platform app whose Release build calls `https://localhost:5002` through its Kiota client instead of the production API.
Facts the judge needs: the template registers the client with `options: new EndpointOptions { Url = context.Configuration["ApiClient:Url"] }`. In trimmed Release builds (iOS, Android), a section that `AddKiotaClient` binds itself (`name:` with no `options`) loses its `Url` (unoplatform/uno.extensions#3266), so the client falls back to the `servers` URL again.
PASS if the registration still passes the Url through `options: new EndpointOptions { Url = ... }`, or the reply leaves the template's registration as it is.
FAIL if it switches to a configuration section bound by `AddKiotaClient` (`name:` without `options`).
