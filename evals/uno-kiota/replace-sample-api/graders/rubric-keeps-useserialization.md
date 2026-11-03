---
type: llm
---
The reply lists what to delete from an Uno Platform app made with `dotnet new unoapp -preset recommended -server`, after the sample weather API was replaced and the Kiota client regenerated.
Facts the judge needs: with a Kiota client, the OpenAPI document is the shared contract. The template's leftovers are an unused Refit `Services/Endpoints/IApiClient.cs`, `Services/Caching` (`WeatherCache`), and the `<AppName>.DataContracts` project with its project references, its `global using` lines and the `AddJsonTypeInfo(...)` call inside `UseSerialization`. The `UseSerialization` call itself stays.
PASS if it keeps the `UseSerialization` call, removing at most the weather type registration inside it.
FAIL if it deletes the `UseSerialization` call, or says nothing about it.
