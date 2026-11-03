---
type: llm
---
The reply sets the redirect URI for OIDC sign-in on the Skia desktop head of an Uno Platform app whose Android and iOS heads sign in with `contoso://callback` from appsettings.json.
Facts the judge needs: a custom scheme is not routed back to a desktop app, so the desktop head needs a loopback `http://localhost:<port>/...` redirect URI (the broker's default path is `/authentication-callback`) registered with the identity provider. The custom scheme stays right for Android and iOS.
PASS if the loopback redirect URI applies to the desktop head only, with Android and iOS still on `contoso://callback`.
FAIL if it changes the redirect URI for every head, or leaves the desktop head on the custom scheme.
