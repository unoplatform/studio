---
type: llm
---
The reply fixes an Uno Platform app whose typed `HttpClient`, registered with `AddHttpClient`, sends no access token although the user is signed in with Uno.Extensions.Authentication.
Facts the judge needs: `UseAuthentication` registers a `DelegatingHandler` that adds the stored access token and refreshes it on a 401. Only Uno's client registrations inside `UseHttp` chain it in: `services.AddClient<TInterface, TImplementation>(context)` for a typed `HttpClient` wrapper, `AddRefitClient<T>(context)` and `AddKiotaClient<T>(context)`. A client from `AddHttpClient` or `new HttpClient()` gets no handler, so the sign-in itself is not the problem.
PASS if it explains that the token is missing because a client from `AddHttpClient` does not get the handler that adds it, which only Uno's client registrations chain in.
FAIL if it blames the sign-in (the token not being stored or not being refreshed) or the API.
