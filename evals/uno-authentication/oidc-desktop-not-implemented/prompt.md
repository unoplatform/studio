---
description: OIDC sign-in that throws NotImplementedException on the Skia desktop head needs an Uno.Extensions version with the desktop loopback broker, and a loopback redirect URI for that head only.
tags: [uno-authentication, oidc-desktop]
runs: 3
max_turns: 10
plugins: ["../../../plugins/uno-platform-studio"]
allowed_tools: [Skill, Read, Glob, Grep]
---
Our Uno Platform app signs in with our Duende IdentityServer through Uno.Extensions' `AddOidc`, configured in appsettings.json:

```json
"OidcAuthentication": {
  "Authority": "https://id.contoso.com",
  "ClientId": "contoso-app",
  "Scope": "openid profile email offline_access",
  "RedirectUri": "contoso://callback",
  "PostLogoutRedirectUri": "contoso://callback"
}
```

Sign-in works on Android and iOS. On the desktop head (`net10.0-desktop`, on Windows), clicking Sign in does nothing and the log shows `Error logging in: System.NotImplementedException`. Our global.json pins `"Uno.Sdk": "7.0.0-dev.77"`. How do we get sign-in working on desktop?
