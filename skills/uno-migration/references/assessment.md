# WPF Migration Assessment

Score a WPF codebase for an Uno Platform migration before anyone writes code. The output is a report with a classification, an effort estimate, the blockers, and a recommended path. Silverlight apps use the same framework; add the Silverlight subsystems from `references/silverlight.md` as dependencies.

## Step 1: Inventory

Collect these facts from the solution:

- Projects, `.cs` files, `.xaml` files, approximate lines of code
- Target framework and project format (SDK-style or classic `.csproj` with `packages.config`)
- Every NuGet package and project reference
- `global.json`, `Directory.Build.props`, `Directory.Packages.props`

Find files with a glob over `**/*.csproj`, `**/*.xaml`, `**/*.cs`, and read each `.csproj` for `TargetFramework` and `PackageReference` items.

## Step 2: Architecture

| Pattern | How to detect | Impact |
|---|---|---|
| MVVM | `ViewModels/` folder, `INotifyPropertyChanged`, `ICommand` | Low: ViewModels port largely unchanged |
| MVVM with CommunityToolkit.Mvvm | `ObservableObject`, `[ObservableProperty]`, `[RelayCommand]` | Low: the same library runs on Uno Platform |
| MVVM Light | `ViewModelLocator`, `SimpleIoc` | Low: move to CommunityToolkit.Mvvm |
| ReactiveUI | `ReactiveObject`, `WhenAnyValue` | Low: the library is cross-platform |
| Prism | `PrismApplication`, `IRegionManager` | Medium: navigation and regions need rework |
| Code-behind | Logic in `.xaml.cs` event handlers | Medium: ports, but every handler is touched |
| No pattern | Static state, singletons, mixed concerns | High: refactor while porting |

## Step 3: Dependencies

Classify each package:

| Class | Meaning | Action |
|---|---|---|
| Portable | .NET Standard or cross-platform .NET | Keep |
| Replaceable | WPF-specific, with an Uno Platform or WinUI equivalent | Map to the equivalent |
| Abstractable | Platform-specific, can sit behind an interface | Interface plus a Windows implementation |
| Blocker | No cross-platform path | Workaround, scope reduction, or keep on Windows |

Common classifications:

| Dependency | Class | Target |
|---|---|---|
| CommunityToolkit.Mvvm, Microsoft.Extensions.*, System.Reactive, EF Core, Newtonsoft.Json | Portable | Keep |
| Entity Framework 6 | Replaceable | EF Core |
| SQL Server LocalDB | Replaceable | SQLite, or a server database behind an HTTP API |
| MahApps.Metro, MaterialDesignInXaml, WPF-UI, iNKORE.UI.WPF.Modern | Replaceable | Uno Themes (Material or Simple) plus Uno Toolkit |
| Dragablz | Replaceable | `TabView` |
| Telerik / DevExpress / Syncfusion WPF | Replaceable | Control-by-control mapping; check the vendor's WinUI or Uno Platform suite |
| Microsoft.AspNet.SignalR (legacy) | Replaceable | Microsoft.AspNetCore.SignalR.Client |
| log4net, NLog | Replaceable or portable | Microsoft.Extensions.Logging (NLog and Serilog have providers) |
| Unity, Autofac, Ninject containers | Replaceable | Microsoft.Extensions.DependencyInjection |
| AvalonEdit | Abstractable or blocker | `TextBox`, or an editor hosted in `WebView2` |
| CefSharp | Blocker | `WebView2` when the use is simple |
| C++/CLI, native DLLs shipped with the app | Blocker | Windows-only implementation behind an interface |

## Step 4: Controls

- **No change beyond the namespace:** `Grid`, `StackPanel`, `Border`, `Canvas`, `TextBlock`, `TextBox`, `PasswordBox`, `Button`, `CheckBox`, `RadioButton`, `ComboBox`, `Slider`, `Image`, `ScrollViewer`, `ListView`, `ItemsControl`, `UserControl`, `ContentControl`, `ToggleButton`, `ProgressBar`.
- **Renamed or reshaped:** `DatePicker` (use `CalendarDatePicker` for a text-box-style picker), `TabControl` (`TabView`, or a Toolkit `TabBar`), `ContextMenu` (`ContextFlyout` + `MenuFlyout`), `Menu` (`MenuBar`), `ToolBar` (`CommandBar`), `ListBox` (`ListView`).
- **Redesign:** `DataGrid`, `StatusBar`, `DockPanel`, `WrapPanel`, `WebBrowser` (`WebView2`), `TreeView` (verify the features you use).
- **High effort or no equivalent:** `RichTextBox` and `FlowDocument` (`RichEditBox` is limited; consider `WebView2`), `DocumentViewer`, `WindowsFormsHost`, `InkCanvas`.

Count usages per bucket. Details and replacements are in `references/wpf-xaml.md`.

## Step 5: Platform APIs

Search the C# for these and count files that use them:

| API | Search for | Impact |
|---|---|---|
| P/Invoke | `DllImport`, `LibraryImport` | High: Windows implementation behind an interface |
| COM interop | `ComImport`, `Marshal.` | High |
| Shell integration, tray icon | `Shell32`, `NotifyIcon`, `TaskbarIcon` | High |
| Global hotkeys | `RegisterHotKey`, `HwndSource.AddHook` | High |
| Registry | `Registry`, `RegistryKey` | Medium: move to settings |
| Printing | `PrintDialog`, `PrintDocument` | Medium |
| Windows Forms | `UseWindowsForms` in the `.csproj`, `System.Windows.Forms` | Medium |
| `Process.Start` for URLs and files | `Process.Start` | Low: `Launcher` |
| Clipboard, file dialogs | `Clipboard.`, `OpenFileDialog`, `SaveFileDialog` | Low: WinUI APIs |

## Step 6: XAML features

Count occurrences of each in `.xaml` files, including resource dictionaries and styles:

| Feature | Search for | Impact |
|---|---|---|
| `x:Static` | `x:Static` | Medium |
| `MultiBinding` | `MultiBinding` | Medium |
| `StringFormat` | `StringFormat` | Medium |
| Style and data triggers | `Style.Triggers`, `DataTrigger`, `<Trigger ` | High |
| Event triggers | `EventTrigger`, `Interaction.Triggers` | Medium |
| `RelativeSource AncestorType` | `AncestorType` | Medium |
| `DynamicResource` | `DynamicResource` | Low: `ThemeResource` or `StaticResource` |
| `clr-namespace:` | `clr-namespace:` | Low: `using:` |

A high trigger count means real XAML rework.

## Step 7: Score

| Dimension | Weight | 5 | 4 | 3 | 2 | 1 |
|---|---|---|---|---|---|---|
| Architecture | 25% | MVVM + DI | MVVM, no DI | Code-behind | Mixed | No structure |
| Dependencies | 20% | All portable | 1-2 replaceable | 3-5 replaceable | Blockers present | Mostly blockers |
| Controls | 20% | No-change and renamed only | Some redesign | `DataGrid` or heavy custom controls | Many high-effort | High-effort dominates |
| Platform coupling | 20% | None | 1-2 abstractable | P/Invoke present | Deep shell integration | The app's purpose is Windows-only |
| XAML compatibility | 10% | Nothing unsupported | A few `x:Static` | `MultiBinding`, `StringFormat` | Trigger-heavy | Pervasive |
| Project health | 5% | SDK-style, .NET 8+ | .NET 6/7 | .NET Core 3.1 | .NET Framework 4.x | .NET Framework < 4.5 |

Overall score is the weighted average, 1.0 to 5.0.

| Score | Class | Meaning |
|---|---|---|
| 4.0-5.0 | Green: strong fit | Full migration. Days for a small app, one to two weeks for a medium one. |
| 3.0-3.9 | Yellow: manageable | Full migration with planning; some areas need redesign. |
| 2.0-2.9 | Orange: challenging | Phased or partial migration. Keep Windows-only features on Windows. |
| 1.0-1.9 | Red: high risk | Fundamental blockers. Consider a partial migration, or not migrating. |

## Step 8: Estimate effort

Start from:

```
Base hours = (XAML files x 0.5) + (C# files that use platform APIs x 2)
+ third-party UI library: 8-16 h
+ each DataGrid view: 4-8 h
+ multi-window to single-window: 4-8 h
+ EF6 to EF Core: 4-8 h
+ each P/Invoke group: 4-16 h
+ each unsupported XAML occurrence: 0.5 h
```

These are heuristics from a handful of assessed apps. State them as an estimate with the assumptions, never as a quote.

## Step 9: Report

```markdown
# Migration Assessment: <App>

## Summary
- Classification: <Green | Yellow | Orange | Red>
- Overall score: <x.x> / 5.0
- Estimated effort: <range>
- Recommended path: <full | phased | partial | do not migrate>

## Scores
| Dimension | Score | Evidence |
|---|---|---|

## Inventory
Projects, files, LOC, target framework, project format.

## Blockers
## Risk areas
## Migration path
Phases, in order, with what is verified at the end of each.

## Effort breakdown
| Phase | Hours | Risk |
|---|---|---|
| Project setup and namespace sweep | | Low |
| Business logic | | Low |
| XAML and UI | | Medium |
| Dependency replacement | | Medium |
| Platform API abstraction | | High |
| Testing and parity pass | | Low |
```

Every score cites the evidence behind it (a file, a package, a count), so a reviewer can check it.

## Quick triage

For a first answer in minutes, check five signals:

1. MVVM? Architecture scores at least 4.
2. MahApps, MaterialDesignInXaml, Telerik, DevExpress? Dependencies score at most 3.
3. `DataGrid`? Controls score at most 3.
4. `DllImport`? Platform coupling scores at most 2.
5. .NET 6 or later? Project health scores at least 4.

Two bad signals mean Yellow or worse; four or more mean Orange or Red.

## Stop signals

Check these before a full assessment:

- **The app's purpose only exists on Windows** (PE-file analysis, driver tools, shell extensions). Migration adds nothing; say so.
- **Native binaries are load-bearing** (C++/CLI layers, bundled native DLLs, CefSharp). Score the rest of the app, and plan those features as Windows-only.

## Calibration examples

Five open-source WPF apps scored with this framework:

| App | Profile | Score | Effort |
|---|---|---|---|
| Calculator (MVVM sample) | Clean MVVM, one portable math library, basic controls, .NET Framework 4.6.1 classic project | 4.7 Green | About 2 h |
| Reservation app (MVVM + EF Core) | MVVM with stores, DI and EF Core + SQLite, no `DataGrid`, modern .NET | 4.5 Green | About 8 h |
| Notepad clone | Code-behind, no packages, `Menu` and `StatusBar`, file dialogs, .NET 6 SDK-style | 4.2 Green | About 6 h |
| Chat client | MVVM with Unity DI, legacy SignalR, MahApps and MaterialDesignInXaml, .NET Framework 4.6 | 3.4 Yellow | About 12 h |
| Keyboard launcher | Mixed MVVM and code-behind, plugin host, tray icon, global hotkeys, file indexing | 2.5 Orange | 2-3 weeks, partial |

The launcher shows the partial case: its UI and plugin contracts port, while the tray, hotkeys, and indexing stay Windows-only behind interfaces.
