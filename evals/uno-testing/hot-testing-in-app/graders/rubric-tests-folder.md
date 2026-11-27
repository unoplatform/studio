---
type: llm
---
The reply adds Hot Testing tests to an Uno Platform app: a test of a `PriceTag` control on its own, and a test that starts the app.
Facts the judge needs: with the `HotTesting` feature, the Uno.Sdk references the test packages in Debug builds only and leaves the project's `Tests` folder (or the folder `HotTestingTestsFolder` names) out of Release builds. A test file anywhere else breaks the Release build with `CS0246` on `TestClass`.
PASS if it puts both test files in the app project's `Tests` folder, or in the folder it sets `HotTestingTestsFolder` to.
FAIL if a test file goes elsewhere in the app project, or into a separate test project.
