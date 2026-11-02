---
type: llm
---
The reply diagnoses an Uno Platform app whose order POST through a Kiota client creates duplicate orders when the server answers 503, and whose checkout hangs about 20 seconds before the error.
Facts the judge needs: `config => config.Options.Add(new RetryHandlerOption { MaxRetry = 0 })` (namespace `Microsoft.Kiota.Http.HttpClientLibrary.Middleware.Options`), or a `ShouldRetry` that returns false, turns Kiota's retries off for one request. Making the order endpoint idempotent stops the duplicates too, but every 503 still waits through the retries, so on its own it leaves the 20-second hang.
PASS if retries are turned off for the order call through that request's options, whether or not the reply also makes the endpoint idempotent.
FAIL if it removes Kiota's retry handler from the client for every call, or relies on idempotency alone.
