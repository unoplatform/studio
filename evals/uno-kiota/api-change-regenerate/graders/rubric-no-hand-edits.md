---
type: llm
---
The reply gets a new model field and a new endpoint from an updated ASP.NET Core API into the generated Kiota client of an Uno Platform app.
Facts the judge needs: the client under `Content/Client` is generated code. `kiota update -o Content/Client/TodoApiClient` (or `dotnet kiota update`) regenerates it with the settings recorded in its `kiota-lock.json`, and anything edited by hand there is lost at the next generation.
PASS if the generated files are changed only by the Kiota CLI.
FAIL if it adds the `DueDate` property or the PATCH method to the generated files by hand, even as a stopgap until the next generation.
