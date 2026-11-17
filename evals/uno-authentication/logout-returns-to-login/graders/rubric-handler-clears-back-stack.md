---
type: llm
---
The reply fixes an Uno Platform app (Uno.Extensions.Authentication and Navigation) whose sign-out and expired session leave the user on the signed-in page.
Facts the judge needs: the template's `InitialNavigate` already passes `Qualifiers.ClearBackStack` on its first navigation; what matters here is the navigation the reply adds for `LoggedOut`. A navigation clears the back stack with `qualifier: Qualifiers.ClearBackStack` or a route that starts with `-/`, so Back cannot return to the signed-in pages.
PASS if the navigation to the login page in the `LoggedOut` handler clears the back stack.
FAIL if that navigation keeps the back stack, so Back returns to MainPage after signing out.
