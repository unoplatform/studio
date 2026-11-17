---
type: llm
---
The reply explains how to add an Uno Toolkit TabBar with Uno.Extensions navigation to an existing Uno Platform app created with `-preset blank` (single project, Uno.Sdk).
Facts the judge needs: in an Uno.Sdk project, `<UnoFeatures>` in the .csproj brings in the packages and their versions; `Toolkit` adds Uno Toolkit and `Navigation` adds Uno.Extensions.Navigation.
PASS if it adds both `Toolkit` and `Navigation` to the `<UnoFeatures>` property in the app's .csproj.
FAIL if its main instruction is to add `PackageReference`s for `Uno.Toolkit.*` / `Uno.Extensions.*` (or `dotnet add package`), or if it adds only one of the two features.
