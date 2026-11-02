---
description: An OIDC session that ends at every restart needs the offline_access scope, not token storage of the app's own.
tags: [uno-authentication, oidc]
runs: 3
max_turns: 10
plugins: ["../../../plugins/uno-platform-studio"]
allowed_tools: [Skill, Read, Glob, Grep]
---
Our Uno Platform app signs in with our Duende IdentityServer through Uno.Extensions' `AddOidc`. Our appsettings.json has:

```json
"OidcAuthentication": {
  "Authority": "https://id.contoso.com",
  "ClientId": "contoso-app",
  "Scope": "openid profile email",
  "RedirectUri": "contoso://callback"
}
```

Sign-in works, but every time the app is closed and reopened, users land on the login page and have to sign in again. I'm planning to save the tokens myself in secure storage after `LoginAsync` and restore them at startup. Is that the right fix?
