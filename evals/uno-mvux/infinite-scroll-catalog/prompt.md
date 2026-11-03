---
description: Infinite scroll over a cursor-based API uses MVUX's cursor-paginated list feed, which carries the next cursor in the page result, instead of a hand-kept cursor field.
tags: [uno-mvux, pagination]
runs: 3
max_turns: 10
plugins: ["../../../plugins/uno-platform-studio"]
allowed_tools: [Skill, Read, Glob, Grep]
---
My Uno Platform app has a catalog page with about 10,000 products. The REST API pages with a cursor: `GET /products?after={cursor}&limit={limit}` returns `{ "items": [...], "nextCursor": "..." }`, and `nextCursor` is null on the last page. Right now the page model loads everything at once and the ListView is slow to appear. I want it to load more items as the user scrolls down. Show the model and the service method.
