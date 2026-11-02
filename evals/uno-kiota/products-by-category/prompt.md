---
description: A service loads products through the injected Kiota client's request builders and query parameters, handles nullable results, and maps them to app records.
tags: [uno-kiota, calling]
runs: 3
max_turns: 10
plugins: ["../../../plugins/uno-platform-studio"]
allowed_tools: [Skill, Read, Glob, Grep]
---
Our Uno Platform app (MVUX) has a Kiota client, `CatalogApiClient`, generated from our API's OpenAPI document into Content/Client and registered in App.xaml.cs. The API has `GET /api/products` with `category` and `top` query parameters, and returns the id, name and price of each product. I need a service the products page Model can use to load the top 20 products in a category. Show the service.
