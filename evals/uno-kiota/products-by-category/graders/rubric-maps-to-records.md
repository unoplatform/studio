---
type: llm
---
The reply writes a service that loads products from a REST API through a generated Kiota client in an Uno Platform app.
Facts the judge needs: Kiota's models are mutable generated classes. MVUX compares the items of a list feed by value, so the service maps the generated models to the app's own records (a `partial record` gets key equality on `Id`) and returns `IImmutableList<T>`, the shape `ListFeed.Async` takes.
PASS if the service maps the generated models to the app's own records before returning them.
FAIL if it returns Kiota's generated model classes to the Model.
