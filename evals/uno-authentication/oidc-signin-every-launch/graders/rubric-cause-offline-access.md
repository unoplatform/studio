---
type: llm
---
The reply diagnoses an Uno Platform app (Uno.Extensions `AddOidc`) whose users must sign in again after every restart.
Facts the judge needs: `AddOidc` stores the access, refresh and ID tokens itself. At startup the template's `InitialNavigate` calls `RefreshAsync`, which renews the session with the refresh token. Duende IdentityServer issues a refresh token only when `Scope` includes `offline_access` and the client allows offline access. Without one, the stored tokens are cleared on the next start.
PASS if it identifies the missing `offline_access` scope, which leaves no refresh token for the startup refresh, as the cause.
FAIL if it says the tokens are not persisted, or blames the redirect URI.
