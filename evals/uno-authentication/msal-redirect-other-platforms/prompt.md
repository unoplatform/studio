---
description: MSAL sign-in that works on Android only drops the shared RedirectUri so each head gets its default, with the iOS URL type and the spa registration.
tags: [uno-authentication, msal]
runs: 3
max_turns: 10
plugins: ["../../../plugins/uno-platform-studio"]
allowed_tools: [Skill, Read, Glob, Grep]
---
We added Microsoft Entra ID sign-in to our Uno Platform app with the recommended template's MSAL option. To get Android working I set `"RedirectUri": "msal<our-client-id>://auth"` in appsettings.development.json and the same scheme on the MsalActivity, and Android signs in now. But on iOS and on WebAssembly, Microsoft's sign-in page shows a redirect URI mismatch error. How do I set up the redirect URIs so every platform works?
