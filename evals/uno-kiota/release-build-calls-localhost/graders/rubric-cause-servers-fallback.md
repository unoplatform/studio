---
type: llm
---
The reply diagnoses an Uno Platform app whose Release build calls `https://localhost:5002` through its Kiota client instead of the production API.
Facts the judge needs: the template reads the client's base address from `ApiClient:Url`, which only `appsettings.development.json` sets, and that file loads only in Debug builds (`UseEnvironment(Environments.Development)` under `#if DEBUG`). With no Url, the generated client's constructor silently falls back to the OpenAPI document's first `servers` URL, the template's `https://localhost:5002`.
PASS if it traces the address to the missing production `Url`: only the development settings set it, so the generated client falls back to the OpenAPI document's `servers` URL.
FAIL if it blames Android networking (the emulator's `localhost`, cleartext traffic) instead.
