---
type: llm
---
The reply explains how to add Hot Design previews of an MVUX page whose model declares `public IListFeed<Product> Items => ListFeed.Async(Products.GetAllAsync);`.
Facts the judge needs: the generated `ProductsViewModelMock.Create(...)` passes null for the model's `IProductService`. A feed written with a method group (`ListFeed.Async(Products.GetAllAsync)`) dereferences `Products` as soon as the property is read, so `Create` throws and the preview renders blank. A lambda (`ListFeed.Async(async ct => await Products.GetAllAsync(ct))`) defers that, and behaves the same at runtime.
PASS if it rewrites the `Items` feed with a lambda, or tells the user that the method group must become a lambda for the mocked previews to work.
FAIL if it leaves the method group unchanged without mentioning it.
