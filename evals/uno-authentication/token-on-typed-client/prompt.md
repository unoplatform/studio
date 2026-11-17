---
description: A typed HttpClient registered with AddHttpClient sends no token; registering it through Uno inside UseHttp lets the authentication handler add it.
tags: [uno-authentication, http]
runs: 3
max_turns: 10
plugins: ["../../../plugins/uno-platform-studio"]
allowed_tools: [Skill, Read, Glob, Grep]
---
My Uno Platform app signs users in with Uno.Extensions authentication (`AddCustom`), and that part works. For our orders API I wrote a typed client, `OrdersClient`, that takes an `HttpClient` in its constructor, and registered it in `ConfigureServices` with `services.AddHttpClient<OrdersClient>(c => c.BaseAddress = new Uri("https://api.contoso.com"))`. Every call returns 401 because no Authorization header is sent. What's the right way to get the signed-in user's token onto these requests?
