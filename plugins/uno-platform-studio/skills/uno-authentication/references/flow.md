# Sign-in Flow with Navigation

How a signed-in or signed-out user moves through the app: the start route, the login page, logout, and an expired session. The route and qualifier mechanics are in the `uno-navigation` skill (`references/qualifiers.md`).

## Start on the right page

`dotnet new unoapp -preset recommended -auth <provider>` generates this in `App.xaml.cs`. Keep it: `RefreshAsync` renews stored tokens and returns whether the user is still signed in.

```csharp
async Task InitialNavigate(IServiceProvider services, INavigator navigator)
{
    var auth = services.GetRequiredService<IAuthenticationService>();
    if (await auth.RefreshAsync())
    {
        await navigator.NavigateViewModelAsync<MainModel>(this, qualifier: Qualifiers.ClearBackStack);
    }
    else
    {
        await navigator.NavigateViewModelAsync<LoginModel>(this, qualifier: Qualifiers.ClearBackStack);
    }

    // Subscribe after the first navigation: a RefreshAsync that cannot renew stored tokens
    // clears them and raises LoggedOut, and the else branch above already goes to login.
    auth.LoggedOut += async (_, _) =>
        await navigator.NavigateViewModelAsync<LoginModel>(this, qualifier: Qualifiers.ClearBackStack);
}

Host = await MainWindow.InitializeNavigationAsync(
    () => Task.FromResult(builder.Build()),
    initialNavigate: InitialNavigate);
```

`RefreshAsync` throws when the provider's refresh does (an `AddWeb` `Refresh` callback that cannot reach the backend, `references/web.md`), and nothing above catches it: wrap the call.

with both pages registered:

```csharp
views.Register(
    new ViewMap<LoginPage, LoginModel>(),
    new ViewMap<MainPage, MainModel>());
routes.Register(
    new RouteMap("Login", View: views.FindByViewModel<LoginModel>()),
    new RouteMap("Main", View: views.FindByViewModel<MainModel>(), IsDefault: true));
```

## Return to login on logout and on an expired session

`LoggedOut` is raised whenever the stored tokens are cleared: an explicit `LogoutAsync`, a `RefreshAsync` that could not renew the session, and an API call whose 401 could not be fixed by a refresh. Handle it in **one** place that lives as long as the app, and navigate there:

- the `InitialNavigate` callback above (the recommended template subscribes nowhere, so without this a logout leaves the user on the signed-in page);
- or the constructor of `ShellModel`/`ShellViewModel` when the app has one (the template does this when it generates a Shell).

Navigating from the handler is safe even when an HTTP call raises it on a background thread. `Qualifiers.ClearBackStack` keeps Back from returning to signed-in pages.

Logout itself then only signs out:

```csharp
public async ValueTask Logout(CancellationToken ct) => await Authentication.LogoutAsync(Dispatcher, ct);
```

Do not also navigate after `LogoutAsync`: the `LoggedOut` handler already does, and a second navigation races it. `LogoutAsync` returns `false`, clearing nothing and raising no event, when MSAL's sign-out is cancelled or `AddWeb` has no `LogoutStartUri`. The OIDC and Web providers have no such path: they clear the tokens and raise `LoggedOut` whatever the browser returned. Since Uno.Extensions 8.0.0-dev.135 a cancelled `CancellationToken` comes out of OIDC's `LogoutAsync` as `OperationCanceledException` after the tokens are cleared, so the logout command catches it (`references/oidc.md`).

## Login page

MVUX Model (template shape, plus error state):

```csharp
public partial record LoginModel(IDispatcher Dispatcher, INavigator Navigator, IAuthenticationService Authentication)
{
    public IState<string> Username => State<string>.Value(this, () => string.Empty);
    public IState<string> Password => State<string>.Value(this, () => string.Empty);
    public IState<string> Error => State<string>.Value(this, () => string.Empty);

    public async ValueTask Login(CancellationToken ct)
    {
        var credentials = new Dictionary<string, string>
        {
            [nameof(Username)] = await Username ?? string.Empty,
            [nameof(Password)] = await Password ?? string.Empty,
        };
        if (await Authentication.LoginAsync(Dispatcher, credentials, cancellationToken: ct))
        {
            await Navigator.NavigateViewModelAsync<MainModel>(this, qualifier: Qualifiers.ClearBackStack);
        }
        else
        {
            await Error.SetAsync("Wrong username or password.", ct);
        }
    }
}
```

Bind the fields with `{Binding Username, Mode=TwoWay}` on a `TextBox` and `{Binding Password, Mode=TwoWay}` on a `PasswordBox` (`Password` property), and the button with `Command="{Binding Login}"`. Rules for states and commands are in the `uno-mvux` skill.

MVVM (`Mvvm` feature, CommunityToolkit.Mvvm): the same calls in an `ObservableObject` with `[ObservableProperty]` fields and an `AsyncRelayCommand`, injecting `IDispatcher`, `INavigator` and `IAuthenticationService` through the constructor.

MSAL, OIDC and Web providers ignore the credentials: their login page is a single "Sign in" button calling `LoginAsync(Dispatcher)`, which opens the provider's browser page.

## Optional sign-in

When most of the app works signed out, start on the home page whatever `RefreshAsync` returns, read `await Authentication.IsAuthenticated()` where the UI differs, and navigate to the login route only from a "Sign in" button or before a protected page. After a successful login, go back with `Navigator.NavigateBackAsync(this)` instead of clearing the back stack.

## What not to build

- A custom `IAuthService`, `SessionService`, or a "current user" singleton holding tokens. `IAuthenticationService` and `ITokenCache` are the session.
- Token storage in settings, files, or `ApplicationData`. The extension stores tokens in the host's `IKeyValueStorage` (Keychain, KeyStore, DPAPI-protected settings, browser storage on WebAssembly).
- A `Frame.Navigate` or page-level check for "is logged in". Use the start route and the `LoggedOut` handler.
