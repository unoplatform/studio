# Tokens on HTTP Calls

`UseAuthentication` registers an HTTP handler that adds the stored access token to outgoing requests and refreshes it on a 401. Do not read the token yourself to set an `Authorization` header, and do not write a `DelegatingHandler` for it.

## Which clients get the token

The handler is registered as a `DelegatingHandler` service. Uno's client registrations chain every registered `DelegatingHandler` into the client:

- `services.AddRefitClient<TInterface>(context)` (`HttpRefit` feature)
- `services.AddKiotaClient<TClient>(context, ...)` (`HttpKiota` feature)
- `services.AddClient<TInterface, TImplementation>(context)` for a typed `HttpClient` wrapper (`Http` feature)

A client from `services.AddHttpClient(...)` or `new HttpClient()` does not get the handler and sends no token. Register clients inside `UseHttp`:

```csharp
.UseHttp((context, services) =>
{
    services.AddRefitClient<IAuthApi>(context);
    services.AddRefitClient<ITodoApi>(context);
})
.UseAuthentication(auth => auth.AddCustom<IAuthApi>(/* see references/custom.md */))
```

`<UnoFeatures>` needs `HttpRefit` or `HttpKiota`; the `Http` feature alone adds only `Uno.Extensions.Http`. The recommended template already lists `HttpKiota`.

## Endpoint configuration

The base address comes from a configuration section in `appsettings.json` (`Url`, and `UseNativeHandler`, default `true`) unless you pass `options: new EndpointOptions { Url = "..." }`.

- **Refit**: the section is the interface name without its leading `I`: `IAuthApi` reads `"AuthApi": { "Url": "https://api.example.com" }`.
- **Kiota**: the section is the `name` argument, which defaults to the client's full type name. Pass `name` to bind a section, for example the template's `ApiClient`: `services.AddKiotaClient<MyApiClient>(context, name: "ApiClient")`.

## How the handler behaves

For each request it sets `Authorization: Bearer <AccessToken>` when an access token is stored. On a `401 Unauthorized` it:

1. calls `IAuthenticationService.RefreshAsync()` once, shared across concurrent requests;
2. retries the request with the new token;
3. if the refresh fails or the retry is still 401, clears the tokens. Clearing raises `LoggedOut`, which is how an expired session sends the user back to the login page (`references/flow.md`).

The token refresh endpoint must not go through that loop: a 401 from it would wait on the refresh it is part of. Mark it with the no-refresh header, and a 401 there clears the tokens instead:

```csharp
[Post("/auth/refresh")]
[Headers(Uno.Extensions.Authentication.Headers.NoRefresh)] // "No-Refresh:true"; the handler strips it before sending
Task<TokenResponse> RefreshAsync([Body] RefreshRequest request, CancellationToken ct);
```

For a Kiota or plain `HttpClient` request, add the header `No-Refresh: true` to that request.

The login call goes through the handler too. With no token stored it is sent without an `Authorization` header, and a 401 there (wrong password) surfaces as the client's exception, which the `Login` callback turns into a failed login.

## Changing how the token is sent

Pass `configureAuthorization` as the second argument of `UseAuthentication`:

```csharp
.UseAuthentication(
    auth => auth.AddCustom(/* ... */),
    configureAuthorization: handlers => handlers.AuthorizationHeader("Token"))   // scheme, default "Bearer"
```

| Call | Effect |
|---|---|
| `handlers.AuthorizationHeader(scheme)` | `Authorization: <scheme> <AccessToken>` |
| `handlers.Cookies("AccessToken", "RefreshToken")` | Sends the tokens as cookies with these names and reads updated tokens from response cookies, instead of the header. The refresh cookie name is optional |
| `handlers.None()` | Registers no handler; nothing adds tokens or refreshes automatically |

Cookies need the `ICookieManager` that `UseHttp` registers, so keep `UseHttp` in the host even if the app has no API client of its own.

## Reading tokens in app code

Inject `ITokenCache` when the app itself needs a token or a claim:

```csharp
var accessToken = await tokenCache.AccessTokenAsync(ct);
var idToken = await tokenCache.TokenAsync(TokenCacheExtensions.IdTokenKey, ct); // MSAL and OIDC store it
```

`ITokenCache.Cleared` fires when the tokens are removed. Decoding an ID token's payload for display or navigation is fine on the client; authorization decisions belong to the API, which validates the access token itself.
