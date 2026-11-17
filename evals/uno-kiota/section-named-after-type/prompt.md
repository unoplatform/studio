---
description: AddKiotaClient with neither name nor options binds the section named after the client's full type name, so a section named after the class is never read; pass the Url as EndpointOptions.
tags: [uno-kiota, register]
runs: 3
max_turns: 10
plugins: ["../../../plugins/uno-platform-studio"]
allowed_tools: [Skill, Read, Glob, Grep]
---
Our Uno Platform app calls two APIs. Besides the template's client, I generated a second Kiota client with `dotnet kiota generate -l CSharp -c BillingApiClient -n MyApp.Billing ...` and registered it in App.xaml.cs, inside `UseHttp`:

```csharp
services.AddKiotaClient<BillingApiClient>(context);
```

appsettings.json has:

```json
"BillingApiClient": { "Url": "https://billing.contoso.com" }
```

But every billing request goes to https://localhost:7070, the `servers` URL in the billing API's OpenAPI document. Why is my setting ignored, and what should the registration look like?
