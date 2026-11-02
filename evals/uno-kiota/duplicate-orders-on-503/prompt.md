---
description: Duplicate orders and a 20-second hang on 503 come from Kiota's retry middleware, turned off for the order call with RetryHandlerOption { MaxRetry = 0 }.
tags: [uno-kiota, retries]
runs: 3
max_turns: 10
plugins: ["../../../plugins/uno-platform-studio"]
allowed_tools: [Skill, Read, Glob, Grep]
---
In our Uno Platform app, checkout calls `await client.Api.Orders.PostAsync(order, cancellationToken: ct)` on our Kiota client, which is registered with `AddKiotaClient`. When the order service is overloaded it answers 503, and we've found customers with the same order created two or three times. The checkout spinner also hangs for about 20 seconds before the error shows. Our code calls `PostAsync` only once. What's going on, and how do we fix it?
