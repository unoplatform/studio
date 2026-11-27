---
type: llm
---
The reply adds Hot Testing tests to an Uno Platform app: a test of a `PriceTag` control on its own, and a test that starts the app.
Facts the judge needs: Hot Testing provides `Assert.IsTrueAsync(() => condition, cancellationToken)`, which retries until the condition holds or times out; `Canvas.ShowAsync` already waits for the element to load, and `App.RunAsync` for the app's first navigation.
PASS if any wait in the tests waits for a condition (such as `Assert.IsTrueAsync`) or relies on `ShowAsync` / `RunAsync` having waited.
FAIL if a test waits with a fixed `Task.Delay` before asserting.
