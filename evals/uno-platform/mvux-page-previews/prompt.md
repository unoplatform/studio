---
description: Hot Design previews of an MVUX page cover its loading, empty and error states with the generated mocks, without adding the HotTesting package by hand, and fix the method-group feed that would leave every preview blank.
tags: [uno-platform, previews]
runs: 3
max_turns: 12
plugins: ["../../../plugins/uno-platform-studio"]
allowed_tools: [Skill, Read, Glob, Grep]
---
My Uno Platform app (Uno.Sdk 7.0, `<UnoFeatures>` has MVUX) has a `ProductsPage` bound to this model:

```csharp
public partial record ProductsModel(IProductService Products)
{
    public IListFeed<Product> Items => ListFeed.Async(Products.GetAllAsync);
}
```

I'd like to see the page in Hot Design's previews panel without it calling the real service, including what it looks like while loading, with no products, and when the service fails. What files do I add, and where?
