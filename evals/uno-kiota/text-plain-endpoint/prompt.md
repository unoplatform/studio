---
description: A text/plain response that throws "expected a application/json content type" is AddKiotaClient's JSON-only registration, fixed with an AddClient registration that keeps Uno's and Kiota's handlers.
tags: [uno-kiota, content-types]
runs: 3
max_turns: 10
plugins: ["../../../plugins/uno-platform-studio"]
allowed_tools: [Skill, Read, Glob, Grep]
---
Our Uno Platform app calls another team's API through a Kiota client registered with `AddKiotaClient`. The JSON endpoints work. But `GET /api/status` returns a plain string (`text/plain`), and `await client.Api.Status.GetAsync()` throws `System.ArgumentOutOfRangeException: Specified argument was out of the range of valid values. (Parameter 'expected a application/json content type')`. We can't change the API. How do I fix this?
