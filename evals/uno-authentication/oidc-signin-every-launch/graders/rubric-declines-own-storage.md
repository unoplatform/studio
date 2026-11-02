---
type: llm
---
The reply diagnoses an Uno Platform app (Uno.Extensions `AddOidc`) whose users must sign in again after every restart.
Facts the judge needs: Uno.Extensions.Authentication already stores the tokens in the host's `IKeyValueStorage` (Keychain, KeyStore, DPAPI-protected settings, browser storage on WebAssembly) and reads them through `ITokenCache`.
PASS if it advises against storing the tokens itself, because Uno.Extensions already persists them.
FAIL if it implements or endorses saving and restoring the tokens in storage of the app's own.
