---
description: Sign-in against the app's own API uses AddCustom, stores only the returned tokens, and leaves the access token to Uno's HTTP handler.
tags: [uno-authentication, custom]
runs: 3
max_turns: 10
plugins: ["../../../plugins/uno-platform-studio"]
allowed_tools: [Skill, Read, Glob, Grep]
---
My Uno Platform app (MVUX, Uno.Extensions hosting) needs a login page. Our backend has `POST /auth/login`, which takes a username and password and returns an access token and a refresh token, and `POST /auth/refresh`, which takes the refresh token. After sign-in, every call to our API has to carry the access token, and users should stay signed in when they restart the app. Show the sign-in setup and how the token gets onto the API calls.
