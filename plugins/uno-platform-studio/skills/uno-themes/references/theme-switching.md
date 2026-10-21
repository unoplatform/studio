# Light/dark switching at runtime

`IThemeService` (`Uno.Extensions.Toolkit`) switches the theme and saves the choice. It comes with the template (`ThemeService` in `UnoFeatures`, registered by `Hosting`/`Extensions`). Decide first which of two behaviours the brief asks for, because the service defaults to the second:

| Brief says | Startup | Toggle |
|---|---|---|
| "remembers the choice" | nothing: the service restores the saved theme itself | `SetThemeAsync(IsDark ? AppTheme.Light : AppTheme.Dark)` |
| "follows the system at startup", nothing about remembering | one `SetThemeAsync(AppTheme.System)` from the model, after `InitializeAsync()` | the same |

## What the service does

- It is **window-bound and navigation-scoped**. The instance a page or model receives is created when navigation initializes, on the window's content. `Host.Services.GetRequiredService<IThemeService>()` in `App.xaml.cs` resolves a *different* instance whose constructor throws; inside `async void OnLaunched` the exception vanishes, so the call looks like it hangs or does nothing.
- On creation it **restores the saved theme** (settings key `CurrentTheme`, default `System`), once the window content has a `XamlRoot`. `SetThemeAsync` waits for that; before the content exists it waits forever.
- `SetThemeAsync(theme)` applies and **saves** `theme`. `AppTheme.System` means follow the OS (`ElementTheme.Default`). `IsDark` is the effective theme; `Theme` is the saved value and reads `System` *before* the restore has run, so a `Theme != System` guard at startup skips the reset.
- `ThemeChanged` fires after an apply that changed `IsDark`. `InitializeAsync()` can be awaited to know the service is ready.

## Rules

1. **No theme code in `App.xaml.cs`.** Not in the host-build callback (blank window, the content never loads), not after `InitializeNavigationAsync` (wrong instance, swallowed exception), not in `initialNavigate`.
2. **Inject `IThemeService` into the model that owns the button** and do everything there.
3. **Do not replace it with `RequestedTheme` on the window root.** The service still restores its saved value onto the same root, and the two fight.
4. **Verify by toggling and asserting; restart once.** Only "follows the system at startup" needs a relaunch to prove, after a toggle to Dark. Hot Reload applies every model and XAML edit here.

## Follow the system at startup, toggle in the header (MVUX)

```csharp
public partial class MainModel
{
    private readonly IThemeService _theme;

    public MainModel(IThemeService theme)
    {
        _theme = theme;
        _theme.ThemeChanged += async (_, _) => await IsDark.SetAsync(_theme.IsDark, CancellationToken.None);
    }

    // Evaluated when the page binds: navigation has created the window's service by then.
    public IState<bool> IsDark => State.Async(this, async ct =>
    {
        await _theme.InitializeAsync();
        await _theme.SetThemeAsync(AppTheme.System);   // drop this line to remember the choice instead
        return _theme.IsDark;
    });

    public IFeed<string> ThemeButtonText => IsDark.Select(static dark => dark ? "Light theme" : "Dark theme");

    public async ValueTask ToggleTheme(CancellationToken ct)
    {
        await _theme.SetThemeAsync(_theme.IsDark ? AppTheme.Light : AppTheme.Dark);
        await IsDark.SetAsync(_theme.IsDark, ct);
    }
}
```

```xml
<Button AutomationProperties.AutomationId="ThemeButton"
        Content="{Binding ThemeButtonText}"
        Command="{Binding ToggleTheme}" />
```

MVVM: the same calls in the view model, started from its constructor with `_ = InitAsync()` where `InitAsync` awaits `InitializeAsync()` then `SetThemeAsync(AppTheme.System)`.

## Symptoms and their one cause

| You see | Because |
|---|---|
| Blank window, empty log | `SetThemeAsync` in the host-build callback, before the window has content |
| Theme call "never completes", no log line after it | A root-provider instance threw inside `async void OnLaunched` |
| `NullReferenceException: Theme service not initialized, InitializeAsync needs to complete` | `SetThemeAsync` before `InitializeAsync()`; await it first |
| App relaunches Dark after a Dark toggle although startup "resets" | The reset ran on the wrong instance, or a `Theme != System` guard skipped it |
| `IsDark` false while the window is Dark | Reading the root provider's instance, not the window's |

Docs: `external/uno.extensions/doc/Learn/ThemeService/HowTo-UseThemeService.md`; the Chefs sample's `SettingsModel` binds an `IState<AppTheme>` to the service with `.ForEach(async (theme, _) => await _themeService.SetThemeAsync(theme))`.
