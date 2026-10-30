---
description: An edited list item is removed and re-added because the record has no key equality; the fix gives it a key.
tags: [uno-mvux, records]
runs: 3
max_turns: 10
plugins: ["../../../plugins/uno-platform-studio"]
allowed_tools: [Skill, Read, Glob, Grep]
---
In my Uno Platform app the products page shows a ListView bound to an MVUX list state of `Product`, defined as `public record Product(int Sku, string Name, decimal Price);`. When the user changes a price I do `await Products.UpdateAllAsync(p => p.Sku == sku, p => p with { Price = price });`. The new price shows, but the row disappears and re-appears with an animation and the ListView loses the selected item. How do I make the edit update the row in place?
