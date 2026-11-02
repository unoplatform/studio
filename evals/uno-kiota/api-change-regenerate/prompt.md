---
description: A new field and endpoint on the server reach the app by refreshing the OpenAPI document and regenerating with the Kiota CLI, not by editing the generated client.
tags: [uno-kiota, generate]
runs: 3
max_turns: 10
plugins: ["../../../plugins/uno-platform-studio"]
allowed_tools: [Skill, Read, Glob, Grep]
---
The backend team added a `dueDate` field to the Todo model and a new `PATCH /api/todos/{id}` endpoint in our ASP.NET Core server project (it uses Swashbuckle). The Kiota client in our Uno Platform app, under Content/Client/TodoApiClient, doesn't have either yet. What's the quickest way to get both into the app?
