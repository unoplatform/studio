---
description: AddKiotaClient needs the HttpKiota feature and the Uno.Extensions.Http.Kiota namespace, not the packages kiota info lists.
tags: [uno-kiota, setup]
runs: 3
max_turns: 10
plugins: ["../../../plugins/uno-platform-studio"]
allowed_tools: [Skill, Read, Glob, Grep]
---
I'm adding a Kiota client to our existing Uno Platform app. It's a single project on Uno.Sdk, and its `<UnoFeatures>` are `Material; Hosting; Http; Mvux; Navigation; ThemeService`. I generated the client into Content/Client/TodoApiClient, and `dotnet kiota info -l CSharp` says to install `Microsoft.Kiota.Bundle`. Before I do that: the registration I added to App.xaml.cs doesn't compile.

```csharp
.UseHttp((context, services) =>
    services.AddKiotaClient<TodoApiClient>(context,
        options: new EndpointOptions { Url = context.Configuration["TodoApi:Url"] }))
```

The error is CS1061: 'IServiceCollection' does not contain a definition for 'AddKiotaClient'. What does the project need?
