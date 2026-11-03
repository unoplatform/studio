---
type: llm
---
The reply diagnoses an Uno Platform app whose Kiota client, generated into the `MyApp.Billing` namespace and registered with `services.AddKiotaClient<BillingApiClient>(context)`, ignores a `"BillingApiClient": { "Url": ... }` section in appsettings.json and calls the OpenAPI document's `servers` URL.
Facts the judge needs: with neither a `name` nor `options`, `AddKiotaClient` binds the section named after the client's full type name, here `MyApp.Billing.BillingApiClient`, so a `BillingApiClient` section is not read. When no Url reaches the client, the generated constructor falls back to the document's first `servers` URL, with no error. `name: "BillingApiClient"` binds that section, and `options: new EndpointOptions { Url = context.Configuration["BillingApiClient:Url"] }` passes the value directly, as the template does.
PASS if it attributes the ignored setting to `AddKiotaClient` reading the section named after the client's full type name instead of `BillingApiClient`.
FAIL if it blames configuration loading, the environment, the OpenAPI document or the generated client, or gives no cause.
