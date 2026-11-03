---
description: A Release build calling localhost:5002 needs the production Url in appsettings.json, still passed as EndpointOptions, instead of the OpenAPI servers fallback.
tags: [uno-kiota, endpoint]
runs: 3
max_turns: 10
plugins: ["../../../plugins/uno-platform-studio"]
allowed_tools: [Skill, Read, Glob, Grep]
---
Our Uno Platform app was created with the recommended template and `-server`, and it loads its data through the generated Kiota client. In Debug everything works. Our production API is https://api.contoso.com. The Release build on Android shows no data, and the logs show requests going to https://localhost:5002. Where does that address come from, and how do I fix it?
