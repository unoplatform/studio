---
type: llm
---
The reply fixes an Uno Platform app (Uno.Extensions.Authentication and Navigation) whose sign-out and expired session leave the user on the signed-in page.
Facts the judge needs: Uno's HTTP handler answers a 401 with one `RefreshAsync` and a retry; when that fails it clears the tokens, which raises `LoggedOut`.
PASS if the reply relies on the `LoggedOut` handler to bring an expired session back to login as well.
FAIL if it adds code that inspects API responses for a 401 (a `DelegatingHandler`, a try/catch around the calls) to navigate to login.
