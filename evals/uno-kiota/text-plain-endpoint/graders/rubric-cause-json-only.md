---
type: llm
---
The reply fixes an Uno Platform app whose Kiota client, registered with `AddKiotaClient`, throws `expected a application/json content type` on an endpoint that returns `text/plain`.
Facts the judge needs: `AddKiotaClient` builds the request adapter with Kiota's JSON parse-node and serialization-writer factories only (unoplatform/uno.extensions#2799). The generated client registers the text, form and multipart factories in Kiota's default registries, which an adapter given explicit factories never reads, so regenerating the client or registering more default factories changes nothing.
PASS if it attributes the exception to `AddKiotaClient` registering only JSON serializers.
FAIL if it blames the generated client or the OpenAPI document, or offers regenerating the client or registering more default factories as the fix.
