---
type: llm
---
The reply diagnoses why `AddOidc` sign-in fails with `NotImplementedException` on the Skia desktop head of an Uno Platform app on Uno.Sdk 7.0.0-dev.77.
Facts the judge needs: `AddOidc`'s default browser calls Uno's `WebAuthenticationBroker.AuthenticateAsync`, which has no implementation on the Skia desktop head before Uno.Extensions 8.0.0-dev.135 and throws `NotImplementedException`. From 8.0.0-dev.135, `AddOidc` registers a desktop loopback broker (`DesktopWebAuthenticationBrokerProvider`) that opens the system browser and receives the redirect on a local HTTP listener. Uno.Sdk 7.0.0-dev.95 and later pin such a version; Uno.Sdk 7.0.0-dev.77 pins Uno.Extensions 8.0.0-dev.85.
PASS if it attributes the exception to Uno's `WebAuthenticationBroker`, which `AddOidc` uses, having no desktop implementation in the Uno.Extensions version the app is on.
FAIL if it blames the identity provider, the `Scope` or the app's own code, or gives no cause.
