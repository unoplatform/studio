---
name: uno-migration
description: "Migrating existing WPF and Silverlight desktop apps to Uno Platform: readiness assessment and scoring, effort estimates, System.Windows to Microsoft.UI.Xaml namespace and API mapping, replacements for x:Static, MultiBinding, StringFormat, Style.Triggers and DataTrigger, WPF control equivalents (DataGrid, Menu, ToolBar, StatusBar, DockPanel), secondary Windows to pages and ContentDialog, Properties.Settings to IWritableOptions, singletons to DI, Windows-only APIs (P/Invoke, hotkeys, tray, registry) behind interfaces, and Silverlight navigation, WCF RIA Services, IsolatedStorage and ChildWindow. Use whenever the user wants to port, convert, modernize or estimate a WPF, Silverlight or .NET Framework XAML desktop app for Uno Platform or WinUI, asks whether an app is a good migration candidate, or pastes WPF XAML or code to translate. Not for building a new app from a brief (use uno-build-app)."
metadata:
  author: uno-platform
  category: migration
---

# Uno Migration

WPF and Silverlight apps move to Uno Platform with most of their C# intact and their XAML reworked: the dialect is WinUI 3, the app has one window, and Windows-only APIs need a home on the other targets. Generic agents port these apps line by line, recreate secondary windows, emulate WPF triggers in code-behind, and convert MVVM apps to a new pattern mid-port. This skill sets the order of work and routes to the reference for each step.

## Workflow

1. **Assess before porting.** Unless the user has already decided and scoped the work, run `references/assessment.md` and report the score, blockers, and path. A Silverlight app also needs `references/silverlight.md`.
2. **Scaffold beside the old app.** Create a new Uno Platform app and move code into it; keep the original building. `references/wpf-architecture.md` has the template command for MVVM and code-behind apps.
3. **Move portable code first:** models, services, utilities, and ViewModels, with the namespace sweep from `references/wpf-api-mapping.md`.
4. **Build the shell and one complete feature** end to end (page, ViewModel, route, service) before porting the rest. Load the `uno-navigation` skill for the shell.
5. **Port the remaining pages,** simplest first, replacing unsupported XAML with `references/wpf-xaml.md` and Windows-only code with `references/wpf-platform-specific.md`.
6. **Verify per feature** on Windows and at least one other target, and track parity by feature area. Use the `uno-testing` skill when the Uno App MCP is available.

Ground API details in the official docs: `uno_platform_docs_search(...)`, then `uno_platform_docs_fetch(sourcePath="…")` with the `sourcePath` from a result. The migration guides are `wpf-migration.md`, `wpf-winui-equivalents.md`, and `guides/silverlight-migration/silverlight-migration-landing.md`.

## Topic map

| Task | Read | Key APIs |
|------|------|----------|
| Decide whether and how to migrate, score an app, estimate effort | `references/assessment.md` | Inventory, six weighted dimensions, Green to Red classes |
| Namespace sweep, API equivalents, input, threading, storage | `references/wpf-api-mapping.md` | `Microsoft.UI.Xaml`, `DispatcherQueue`, `Launcher`, pickers, `KeyboardAccelerator` |
| Replace `x:Static`, `MultiBinding`, `StringFormat`, triggers, `AncestorType`; map WPF controls; theme keys | `references/wpf-xaml.md` | `x:Bind` functions, `VisualStateManager`, `StateTrigger`, `DataGrid`, `MenuBar`, `CommandBar` |
| Target project, MVVM vs MVUX, windows to pages and dialogs, settings, theme switching, DI | `references/wpf-architecture.md` | `-presentation mvvm`, `ContentDialog`, `IWritableOptions<T>`, `IThemeService` |
| P/Invoke, hotkeys, tray, screen capture, OCR, `Process.Start`, WinAppSDK vs desktop head | `references/wpf-platform-specific.md` | Service interfaces, `OperatingSystem.IsWindows()`, `win:` XAML prefix |
| Silverlight navigation, RIA Services, IsolatedStorage, ChildWindow, Silverlight Toolkit | `references/silverlight.md` | Routes with data, Kiota, Refit, `ApplicationData` |

## Critical rules

- **Keep the app's presentation pattern.** A WPF MVVM app becomes an MVVM Uno Platform app (`-presentation mvvm`, CommunityToolkit.Mvvm) and its ViewModels port with namespace changes. Move to MVUX only when the user asks; changing platform and pattern at once doubles the risk.
- **One window, many pages.** Secondary windows become pages reached by routes; modal windows and `MessageBox` become `ContentDialog` (with `XamlRoot` set) or dialog routes. Do not create extra `Window` instances to mimic WPF.
- **Use the WinUI replacement for each unsupported XAML feature.** `x:Static` becomes a resource or a static `x:Bind`; `StringFormat` and `MultiBinding` become `x:Bind` functions, `Run`s, or a computed property; triggers become `VisualStateManager` with `StateTrigger`. Do not rebuild triggers as code-behind property setters.
- **Navigation comes from `uno-navigation`, not from the WPF app.** Use its shell templates and region patterns as written; do not port `Frame.Navigate` or Prism regions structurally.
- **Restyle through `uno-themes`.** WPF theme libraries do not carry over. Keep the new app's theme and use semantic keys (`OnSurfaceBrush`, `BodyMedium`, `FilledButtonStyle`); Fluent keys such as `BodyTextBlockStyle` are not defined by Uno Material or Simple.
- **Windows-only code sits behind an interface.** Register a Windows implementation and a fallback, pass portable types (`Stream`, `byte[]`, `CultureInfo`) across the boundary, and hide UI for features the target cannot perform. `#if WINDOWS` and the `win:` prefix select only the WinAppSDK head, not the desktop head running on Windows.
- **No blocking on the UI thread.** Replace `Dispatcher.Invoke` with `DispatcherQueue.TryEnqueue`, and `.Result`, `.Wait()`, and `Thread.Sleep` with `await`; WebAssembly runs the UI on one thread.
- **Settings go through `IWritableOptions<T>`**, not the registry, `Properties.Settings`, or files next to the executable. Local files go in `ApplicationData.Current.LocalFolder`.
- **A feature is migrated when it works, not when its page exists.** Compare menus, toolbars, shortcuts, and settings element by element against the WPF app, and report parity per feature area.

## Related skills

- `uno-platform` for project-wide rules and `<UnoFeatures>`.
- `uno-navigation` for the shell, routes, regions, dialogs, and passing data.
- `uno-themes` for colors, typography, and control styles.
- `uno-toolkit` for `TabBar`, `NavigationBar`, `AutoLayout`, `ZoomContentControl`, `CommandExtensions`, and `AncestorBinding`.
- `uno-mvux` only when the user chose MVUX for the migrated app.
- `uno-testing` to verify migrated features in the running app.
- `uno-build-app` for new features added after the port.
