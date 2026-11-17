---
description: After the template's sample API is replaced, the unused Refit IApiClient, Services/Caching and the DataContracts project go, and UseSerialization stays.
tags: [uno-kiota, template]
runs: 3
max_turns: 10
plugins: ["../../../plugins/uno-platform-studio"]
allowed_tools: [Skill, Read, Glob, Grep]
---
Our Uno Platform app was created with `dotnet new unoapp -preset recommended -server`, so it calls the server project through the generated Kiota client. We replaced the template's sample weather API in the server with our own Todo API, saved its swagger.json to Specs/ and regenerated the client with `dotnet kiota update`. The solution still has the template's weather code. What can we delete, and is there anything that looks unused but has to stay?
