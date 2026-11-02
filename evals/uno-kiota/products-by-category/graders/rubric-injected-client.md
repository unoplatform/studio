---
type: llm
---
The reply writes a service that loads products from a REST API through a generated Kiota client in an Uno Platform app.
Facts the judge needs: the client is registered with `AddKiotaClient` inside `UseHttp` and injected into the service. It mirrors the API's paths: `client.Api.Products.GetAsync(config => { config.QueryParameters.Category = category; config.QueryParameters.Top = 20; }, ct)`.
PASS if the service calls the operation through the client's request builders (`client.Api.Products.GetAsync(...)`).
FAIL if it builds the URL or the query string itself, or sends a hand-built request through the client's request adapter.
