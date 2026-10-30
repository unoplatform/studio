# Custom Authentication

`AddCustom` is for an app that signs in against its own backend (username and password, an API key, a device code) or, in a prototype, checks credentials locally. You supply the login, refresh and logout callbacks; the extension stores what they return, applies the access token to HTTP calls, and raises `LoggedOut`. Verified with Uno.Samples `UI/Authentication.CustomExtensionsDemo` (Kiota, Uno.Sdk 7.0.0-dev.115).

Feature: `Authentication` in `<UnoFeatures>`, plus `HttpRefit` or `HttpKiota` when the callbacks call a backend (`Http` alone brings no Refit or Kiota).

## How the result is interpreted

- `IAuthenticationService.IsAuthenticated()` is true while **any** token is stored. Whatever dictionary `Login` returns is saved as-is, every key, to the host's `IKeyValueStorage`.
- **Return a new dictionary that holds only tokens.** The template returns the `credentials` dictionary it received, which saves the user's password next to the tokens; so does the docs walkthrough's Refit example, which also lists only the `Http` feature. Use the `TokenCacheExtensions` keys (`AccessTokenKey`, `RefreshTokenKey`, `IdTokenKey`), which the HTTP handler and `ITokenCache` read.
- Return `null` (or an empty dictionary) to fail the login: `LoginAsync` returns `false`.
- An exception thrown in a callback is rethrown by `LoginAsync`/`RefreshAsync`/`LogoutAsync`; `RefreshAsync` keeps the tokens, `LogoutAsync` clears them and raises `LoggedOut` first. Return `null` for wrong credentials; let real failures through.
- Without a `Refresh` callback, `RefreshAsync` returns `true` whenever tokens are stored, so a startup refresh treats an expired token as signed in until an API call returns 401. A `Refresh` that returns `null` clears the tokens, which raises `LoggedOut`.
- Without a `Logout` callback, `LogoutAsync` clears the tokens. Return `false` from `Logout` to cancel the sign-out (tokens are kept).

## Registration

In `App.xaml.cs`, inside `.Configure(host => host ...)`. The `name` (template: `"CustomAuth"`) only matters with several providers: `LoginAsync(dispatcher, credentials, provider: "CustomAuth")`.

```csharp
.UseAuthentication(auth => auth.AddCustom<IAuthApi>(custom => custom
    .Login(async (api, dispatcher, credentials, ct) =>
    {
        var username = credentials.TryGetValue("Username", out var u) ? u : string.Empty;
        var password = credentials.TryGetValue("Password", out var p) ? p : string.Empty;
        try
        {
            var response = await api.LoginAsync(new LoginRequest(username, password), ct);
            return new Dictionary<string, string>
            {
                [TokenCacheExtensions.AccessTokenKey] = response.AccessToken,
                [TokenCacheExtensions.RefreshTokenKey] = response.RefreshToken,
            };
        }
        catch (ApiException ex) when (ex.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.BadRequest)
        {
            return null; // wrong credentials: LoginAsync returns false
        }
    })
    .Refresh(async (api, tokens, ct) =>
    {
        if (!tokens.TryGetValue(TokenCacheExtensions.RefreshTokenKey, out var refreshToken))
        {
            return null;
        }
        var response = await api.RefreshAsync(new RefreshRequest(refreshToken), ct);
        return new Dictionary<string, string>
        {
            [TokenCacheExtensions.AccessTokenKey] = response.AccessToken,
            [TokenCacheExtensions.RefreshTokenKey] = response.RefreshToken,
        };
    }),
    name: "CustomAuth"))
```

`AddCustom<TService>` resolves `TService` from DI for each callback, so register the client in `UseHttp` (see `references/http.md`). With Kiota, the template's default, `TService` is the generated client; `references/http.md` has its error type and no-refresh header. The Refit contract for the calls above, including the header that keeps the refresh call out of the token-refresh loop:

```csharp
public interface IAuthApi
{
    [Post("/auth/login")]
    Task<TokenResponse> LoginAsync([Body] LoginRequest request, CancellationToken ct);

    [Post("/auth/refresh")]
    [Headers(Uno.Extensions.Authentication.Headers.NoRefresh)]
    Task<TokenResponse> RefreshAsync([Body] RefreshRequest request, CancellationToken ct);
}
```

Write `Uno.Extensions.Authentication.Headers` in full: Refit's attribute is also called `Headers`.

Local check, no backend (a prototype: say so in the report):

```csharp
.UseAuthentication(auth => auth.AddCustom(custom => custom
    .Login((sp, dispatcher, credentials, ct) =>
        ValueTask.FromResult<IDictionary<string, string>?>(
            credentials.TryGetValue("Username", out var name) && !string.IsNullOrEmpty(name)
                ? new Dictionary<string, string> { [TokenCacheExtensions.AccessTokenKey] = "local-session" }
                : null))))
```

## Callback signatures

Each callback also takes a final `CancellationToken`:

| Builder | `Login` receives | `Refresh` receives | `Logout` receives, returns `bool` |
|---|---|---|---|
| `AddCustom` | `(sp, dispatcher, credentials)` or `(sp, dispatcher, tokenCache, credentials)` | `(sp, tokens)` or `(sp, tokenCache, tokens)` | `(sp)`, `(sp, dispatcher)`, `(sp, dispatcher, tokens)`, `(sp, dispatcher, tokenCache, tokens)` |
| `AddCustom<TService>` | `(service, credentials)`, `(service, dispatcher, credentials)`, `(service, dispatcher, tokenCache, credentials)`, `(service, sp, dispatcher, tokenCache, credentials)` | `(service, tokens)`, `(service, tokenCache, tokens)`, `(service, sp, tokenCache, tokens)` | `(service, tokens)`, `(service, dispatcher, tokens)`, `(service, dispatcher, tokenCache, tokens)`, `(service, sp, dispatcher, tokenCache, tokens)` |

`Login` and `Refresh` return `ValueTask<IDictionary<string, string>?>`; use an `async` lambda or `ValueTask.FromResult`.

The credential keys are whatever the login Model passes to `LoginAsync`; the template's are `"Username"` and `"Password"`.
