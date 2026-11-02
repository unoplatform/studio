---
type: llm
---
The reply diagnoses an Uno Platform app whose order POST through a Kiota client creates duplicate orders when the server answers 503, and whose checkout hangs about 20 seconds before the error.
Facts the judge needs: Kiota's retry middleware, which `AddKiotaClient` attaches, resends a request that got 503 (also 429 and 504) up to three times before the call throws, waiting the `Retry-After` delay or, without one, 3, 6 and 12 seconds: about 21 seconds in all.
PASS if it attributes both symptoms to Kiota's retry middleware resending the 503 request.
FAIL if it blames double taps, the UI, or the server alone.
