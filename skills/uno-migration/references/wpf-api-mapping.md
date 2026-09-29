# WPF Namespace and API Mapping

The mechanical first pass of a WPF migration: namespaces, then the APIs whose shape changed. The official equivalents table is `wpf-winui-equivalents.md` in the Uno docs; fetch it for anything not listed here.

## Namespace sweep

Replace sub-namespaces before the root, or `System.Windows.Controls` turns into `Microsoft.UI.Xaml.Xaml.Controls`.

```
1.  System.Windows.Threading    ->  Microsoft.UI.Dispatching
2.  System.Windows.Controls     ->  Microsoft.UI.Xaml.Controls
3.  System.Windows.Media        ->  Microsoft.UI.Xaml.Media
4.  System.Windows.Input        ->  Microsoft.UI.Xaml.Input
5.  System.Windows.Data         ->  Microsoft.UI.Xaml.Data
6.  System.Windows.Shapes       ->  Microsoft.UI.Xaml.Shapes
7.  System.Windows.Documents    ->  Microsoft.UI.Xaml.Documents
8.  System.Windows.Navigation   ->  Microsoft.UI.Xaml.Navigation
9.  System.Windows.Automation   ->  Microsoft.UI.Xaml.Automation
10. System.Windows              ->  Microsoft.UI.Xaml            (last)
```

In XAML, change `xmlns:local="clr-namespace:MyApp.Views"` to `xmlns:local="using:MyApp.Views"`. The default `xmlns` and `xmlns:x` URIs are the same as WPF; leave them alone. Check string-based type names in configuration too.

`Clipboard`, `DataPackage`, `Launcher`, `ApplicationData`, and the pickers live in `Windows.*` namespaces (`Windows.ApplicationModel.DataTransfer`, `Windows.System`, `Windows.Storage`, `Windows.Storage.Pickers`), which Uno Platform implements on every target.

## Core APIs

| WPF | Uno Platform | Notes |
|---|---|---|
| `Window` (secondary) | `Page` or `ContentDialog` | One window per app on mobile and WebAssembly; see `references/wpf-architecture.md` |
| `Window.ShowDialog()`, `MessageBox.Show()` | `ContentDialog.ShowAsync()` | Set `XamlRoot` first |
| `Dispatcher.Invoke`, `Dispatcher.BeginInvoke` | `DispatcherQueue.TryEnqueue` | No synchronous invoke. `EnqueueAsync` from CommunityToolkit.WinUI awaits the result |
| `Dispatcher.CheckAccess()` | `DispatcherQueue.HasThreadAccess` | |
| `RoutedUICommand`, `CommandBinding` | `ICommand` (`[RelayCommand]`), or `XamlUICommand` / `StandardUICommand` | WPF command routing does not exist |
| `InputBindings`, `KeyBinding` | `KeyboardAccelerator` on the control | Fires the action; WPF `InputGestureText` was display only |
| `ApplicationCommands.Copy/Paste/Undo` | `StandardUICommand` with `StandardUICommandKind` | `TextBox` has `Undo()`, `Redo()`, `CanUndo` built in |
| `Clipboard.SetText(s)` | `var p = new DataPackage(); p.SetText(s); Clipboard.SetContent(p);` | Text works on every target; other formats vary |
| `OpenFileDialog`, `SaveFileDialog` | `FileOpenPicker`, `FileSavePicker` | On the WinAppSDK head, call `InitializeWithWindow` with the window handle |
| `Process.Start(url)` with `UseShellExecute` | `await Launcher.LaunchUriAsync(new Uri(url))` | Also `mailto:`. On WebAssembly, call it straight from the user gesture with no `await` before it |
| `Process.Start("app.exe")` | Windows-only implementation behind an interface | See `references/wpf-platform-specific.md` |
| `Properties.Settings.Default` | `IWritableOptions<T>` | See `references/wpf-architecture.md` |
| `Application.Current.MainWindow` | The `Window` the app creates in `OnLaunched` | Keep a reference; there is no `MainWindow` property |
| `DynamicResource` | `ThemeResource` (theme-dependent) or `StaticResource` | |
| `ContextMenu` | `ContextFlyout` with `MenuFlyout` | |
| `ToolTip="..."` | `ToolTipService.ToolTip="..."` | |
| `Visibility.Hidden` | `Visibility.Collapsed`, or `Opacity="0"` to keep layout | WinUI has only `Visible` and `Collapsed` |

## Input

Pointer events unify mouse, touch, and pen.

| WPF | WinUI / Uno Platform |
|---|---|
| `MouseDown`, `MouseMove`, `MouseUp` | `PointerPressed`, `PointerMoved`, `PointerReleased` |
| `MouseEnter`, `MouseLeave` | `PointerEntered`, `PointerExited` |
| `MouseWheel` | `PointerWheelChanged` |
| `CaptureMouse()`, `ReleaseMouseCapture()` | `CapturePointer(e.Pointer)`, `ReleasePointerCapture(e.Pointer)` |
| `e.GetPosition(element)` | `e.GetCurrentPoint(element).Position` |
| `MouseDoubleClick` | `DoubleTapped` |
| `KeyDown` with `Key` | `KeyDown` with `VirtualKey` (enum values differ) |
| `PreviewKeyDown` and other tunneling events | `PreviewKeyDown` exists for keys; most other preview events do not. Use `AddHandler(..., handledEventsToo: true)` |

## Threading

| WPF | Uno Platform |
|---|---|
| `BackgroundWorker` | `Task.Run` with `async`/`await` |
| `task.Result`, `task.Wait()` on the UI thread | `await`. WebAssembly runs the UI on a single thread, so blocking on it hangs or throws |
| `Thread.Sleep` | `await Task.Delay` |
| `DispatcherTimer` | `DispatcherTimer` (same name, `Microsoft.UI.Xaml`), or `DispatcherQueue.CreateTimer()` |

## Data and storage

| WPF | Uno Platform |
|---|---|
| Files next to the executable, `%AppData%` paths | `ApplicationData.Current.LocalFolder`. `System.IO` works on `LocalFolder.Path`; on WebAssembly, await one storage call (for example `CreateFolderAsync`) before the first `System.IO` read |
| `IsolatedStorage` | `ApplicationData.Current.LocalFolder` |
| Registry | Settings through `IWritableOptions<T>` |
| WCF client | gRPC, or REST with Kiota or Refit (`UseHttp`) |
| Direct SQL Server connection | SQLite locally; a server database only behind an HTTP API on mobile and WebAssembly |

## Checklist after the sweep

- No `System.Windows` imports remain in `.cs` files
- No `clr-namespace:` remains in `.xaml` files
- No `Dispatcher.Invoke`, `MessageBox.Show`, `Thread.Sleep`, or `.Result` on UI code paths
- Unsupported XAML is handled (`references/wpf-xaml.md`)
- The solution builds for at least the desktop target
