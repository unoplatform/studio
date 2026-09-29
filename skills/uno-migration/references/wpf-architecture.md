# WPF Architecture: Windows, Navigation, Settings, Services

How the structure of a WPF app maps to an Uno Platform app. Navigation, MVUX, and theme details live in their own skills; this file covers the migration decisions and points to them.

## Create the target project

Scaffold the Uno Platform app next to the WPF solution and port into it; do not convert the WPF project in place.

```bash
# WPF app built on MVVM: keep MVVM, and the ViewModels come across nearly unchanged
dotnet new unoapp -o MyApp -preset recommended -presentation mvvm

# Code-behind app, or the user asked to move to MVUX
dotnet new unoapp -o MyApp -preset recommended
```

The recommended preset includes Uno.Extensions (hosting, DI, configuration, navigation), Uno Toolkit, and the Simple theme. Add `-theme material` when the WPF app used a Material library or the user wants Material Design. Build it before porting anything.

Move portable code (models, services, utilities, ViewModels) first. A shared class library that both the WPF app and the Uno Platform app reference lets the two run side by side while pages move over.

## Presentation pattern

- **MVVM in WPF stays MVVM.** CommunityToolkit.Mvvm runs unchanged. Hand-written `INotifyPropertyChanged` base classes and `RelayCommand` implementations also work; replace them with CommunityToolkit.Mvvm only when that is cheaper than keeping them. Do not convert ViewModels to MVUX Models unless the user asks.
- **Code-behind apps** can keep their code-behind for the first port, then move logic into ViewModels (or MVUX Models, for a new-style app) page by page. Porting and re-architecting at once doubles the risk.
- **MVVM Light, Prism, Caliburn.Micro:** move ViewModels to CommunityToolkit.Mvvm, and move Prism regions and `IRegionManager` navigation to Uno.Extensions Navigation.

## Windows become pages and dialogs

Mobile and WebAssembly have one window. Plan the app as one window hosting pages.

| WPF | Uno Platform |
|---|---|
| `MainWindow` with a sidebar and a `Frame` | A shell page with a `NavigationView` or `TabBar` and a content region |
| Secondary `Window` with its own workflow | A `Page` reached by a route |
| Modal `Window.ShowDialog()` | `ContentDialog`, or a dialog route with Uno.Extensions Navigation |
| `MessageBox.Show` | `ContentDialog`, or `IMessageDialogService` / `ShowMessageDialogAsync` with Uno.Extensions Navigation |
| Floating tool window | A pane on the page (`SplitView`, a `TwoPaneView`, a flyout) |
| `Page` inside a `Frame` inside a `Window` | Same idea: a `Page` in a region |
| Reusable fragment | `UserControl` |

For the shell, routes, regions, and dialogs, load the `uno-navigation` skill and read `references/setup.md`, `references/routes.md`, and the shell template that matches the WPF layout (`references/shell-navigationview-template.md` for a sidebar, `references/shell-tabbar-template.md` for tabs). Use its region pattern as written; do not port WPF `Frame.Navigate` calls into the new shell.

A `ContentDialog` needs its `XamlRoot` before it can show:

```csharp
// WPF
var dialog = new FindReplaceWindow { Owner = this };
if (dialog.ShowDialog() == true) { Apply(dialog.SearchTerm); }

// Uno Platform
var dialog = new FindReplaceDialog { XamlRoot = this.XamlRoot };
if (await dialog.ShowAsync() == ContentDialogResult.Primary) { Apply(dialog.SearchTerm); }
```

- WPF `DialogResult = true` maps to the primary button; `false` maps to the close button.
- To keep a dialog open (validation, "Reset defaults"), set `args.Cancel = true` in the button's click handler.
- Only one `ContentDialog` can be open at a time. Close the first one before showing a confirmation.

## Settings

`Properties.Settings.Default` and registry-backed settings become an immutable record read and written through `IWritableOptions<T>` from Uno.Extensions Configuration.

```csharp
public record AppSettings
{
    public bool ShowConfirmation { get; init; } = true;
    public int FontSize { get; init; } = 14;
    public bool FirstRun { get; init; } = true;
}
```

Register the section in the host builder. `Section<T>()` registers both `IOptions<T>` and `IWritableOptions<T>`:

```csharp
.UseConfiguration(configure: config => config
    .EmbeddedSource<App>()
    .Section<AppSettings>())
```

An `appsettings.json` is optional; it only supplies defaults. Updates are persisted to the app data folder and survive a relaunch.

In an MVVM ViewModel:

```csharp
public partial class SettingsViewModel(IWritableOptions<AppSettings> settings) : ObservableObject
{
    [ObservableProperty]
    public partial bool ShowConfirmation { get; set; } = settings.Value.ShowConfirmation;

    async partial void OnShowConfirmationChanged(bool value) =>
        await settings.UpdateAsync(s => s with { ShowConfirmation = value });
}
```

In an MVUX Model, every `State` factory takes the owner as its first argument:

```csharp
public partial record SettingsModel(IWritableOptions<AppSettings> Settings)
{
    public IState<bool> ShowConfirmation => State.Value(this, () => Settings.Value.ShowConfirmation)
        .ForEach(async (value, ct) => await Settings.UpdateAsync(s => s with { ShowConfirmation = value }));
}
```

Bind `ToggleSwitch.IsOn` two-way to the property or state. Handling `Toggled` in code-behind fires during initialization and writes the loaded value straight back.

Fetch `external/uno.extensions/doc/Learn/Configuration/HowTo-WritableConfiguration.md` for the current API.

## Theme switching

WPF apps swap resource dictionaries at run time. In an Uno.Extensions app, inject `IThemeService` (namespace `Uno.Extensions.Toolkit`) and call `await themeService.SetThemeAsync(AppTheme.Dark)`; the choice persists across sessions. Without Uno.Extensions, `Uno.Toolkit.UI.SystemThemeHelper.SetApplicationTheme(xamlRoot, ElementTheme.Dark)` switches it.

Do not declare an interface of your own named `IThemeService` in an Uno.Extensions app.

## Singletons become DI services

```csharp
// WPF
var history = Singleton<HistoryService>.Instance;

// Uno Platform
public interface IHistoryService
{
    Task<IImmutableList<HistoryEntry>> GetEntriesAsync(CancellationToken ct);
    Task AddAsync(HistoryEntry entry, CancellationToken ct);
}

.ConfigureServices((context, services) =>
    services.AddSingleton<IHistoryService, FileHistoryService>())

public partial class HistoryViewModel(IHistoryService history) : ObservableObject { }
```

Retire `Singleton<T>.Instance`, `App.Current.Properties`, and static service locators as each consumer moves; do not keep both paths alive. The template's `App.Host` is available when code-behind genuinely needs a service, but constructor injection into the ViewModel or Model is the default.

## First-run and startup flows

A WPF `FirstRunWindow` shown before `MainWindow` becomes either a route the app navigates to first (a login-style flow, see `uno-navigation`) or a `ContentDialog` shown once the shell has loaded. Read the flag from `IWritableOptions<AppSettings>` and clear it with `UpdateAsync` when the flow completes.

## Parity tracking

Mark a feature migrated when it works end to end on Windows and one other target, not when its page exists. Compare the WPF UI element by element: every menu item, toolbar button, keyboard shortcut, and settings toggle. Track parity per feature area so scaffolded-but-empty pages show up.

## Common mistakes

- Recreating the multi-window layout with extra `Window` instances.
- Converting an MVVM app to MVUX as part of the platform port.
- Keeping static singletons alongside DI.
- Handling settings toggles in code-behind event handlers instead of binding.
- Declaring a phase done when its pages render but their commands do nothing.
