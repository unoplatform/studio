---
type: llm
---
The reply says what a single-project Uno Platform app on Uno.Sdk needs before `AddKiotaClient` compiles, after `kiota info` listed `Microsoft.Kiota.Bundle` to install.
Facts the judge needs: the `HttpKiota` entry in `<UnoFeatures>` brings Uno.Extensions.Http.Kiota and the Kiota runtime packages (the abstractions, the HTTP library, and the JSON, text, form and multipart serializers), so the packages `kiota info` lists are not needed. The `Http` feature alone adds neither Kiota nor Refit. `AddKiotaClient` is in the `Uno.Extensions.Http.Kiota` namespace.
PASS if it says the packages `kiota info` lists are not needed, because the `HttpKiota` feature already brings the Kiota runtime.
FAIL if it tells the developer to install `Microsoft.Kiota.Bundle` or other Kiota packages, or says nothing about them.
