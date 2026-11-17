---
type: llm
---
The reply fixes an Uno Platform app (Uno.Extensions.Authentication and Navigation) whose sign-out and expired session leave the user on the signed-in page.
Facts the judge needs: a `LoggedOut` handler navigates to login on every sign-out, so a second navigation from the sign-out command races it.
PASS if the sign-out command leaves the navigation to the `LoggedOut` handler, with no navigation of its own after `LogoutAsync`.
FAIL if the command also navigates to the login page after `LogoutAsync`.
