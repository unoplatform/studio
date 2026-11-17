---
type: llm
---
The reply fixes an Uno Platform app whose Kiota client, registered with `AddKiotaClient`, throws `expected a application/json content type` on an endpoint that returns `text/plain`.
Facts the judge needs: `AddKiotaClient` also attaches Kiota's middleware (retry on 503, 429 and 504, redirects, the user agent). A replacement registration keeps it by adding each type from `KiotaClientFactory.GetDefaultHandlerActivatableTypes()` as a service and to the client with `AddHttpMessageHandler`. Without that, the 503 retry is gone.
PASS if Kiota's middleware still applies to the client's calls: the replacement registration attaches the default Kiota handlers, or `AddKiotaClient` stays in place for the client that keeps using it.
FAIL if the replacement registration drops Kiota's middleware.
