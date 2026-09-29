# Windows-Only Features in a Cross-Platform Port

WPF apps call Win32, COM, the shell, and the registry freely. In the port, each of those sits behind an interface with a Windows implementation and a fallback, and the UI hides what a target cannot do.

## Which "Windows" you are on

An Uno Platform app can run on Windows through two heads:

- **WinAppSDK** (`net10.0-windows10.0.x` target): `#if WINDOWS` in C# and the `win:` XAML prefix select it.
- **Desktop** (`net10.0-desktop` target, Skia renderer): runs on Windows, macOS, and Linux. `#if WINDOWS` and `win:` do **not** select it, even on a Windows machine.

So a Windows-only feature needs two checks: compile-time for the WinAppSDK head, and a run-time `OperatingSystem.IsWindows()` check on the desktop head. Ask which heads the app ships before choosing; many migrated apps ship the desktop head on Windows.

## Service abstraction

Define an interface in shared code, implement it per platform, and register the right one at startup:

```csharp
public interface IGlobalHotKeyService
{
    bool IsSupported { get; }
    void Register(int id, VirtualKeyModifiers modifiers, VirtualKey key);
    event EventHandler<int> Pressed;
}

.ConfigureServices((context, services) =>
{
    if (OperatingSystem.IsWindows())
        services.AddSingleton<IGlobalHotKeyService, Win32GlobalHotKeyService>();
    else
        services.AddSingleton<IGlobalHotKeyService, UnsupportedGlobalHotKeyService>();
})
```

The unsupported implementation reports `IsSupported = false`; the ViewModel exposes that, and the page binds the feature's visibility to it. Do not leave a button that does nothing on some targets.

Pass portable types across the interface: `Stream` or `byte[]` for images (never `SoftwareBitmap` or `System.Drawing.Bitmap`), `CultureInfo` for languages (not `Windows.Globalization.Language`), a path or `Stream` for files. Convert inside the platform implementation.

Put platform-specific code in the `Platforms/<Platform>` folders or behind `#if`, and platform-only packages behind a target-framework condition:

```xml
<ItemGroup Condition="$(TargetFramework.Contains('-windows'))">
  <PackageReference Include="SomeWindowsOnlyPackage" />
</ItemGroup>
```

A Win32 P/Invoke (`user32.dll`) compiles on every target but only runs on Windows; guard the call with `OperatingSystem.IsWindows()`.

## Common Windows-only features

| WPF feature | In the port |
|---|---|
| `Process.Start(url)` | `Launcher.LaunchUriAsync`, works everywhere |
| `Process.Start("tool.exe")`, `CliWrap` | Windows (or desktop) implementation behind an interface; hidden on mobile and WebAssembly |
| Registry | `IWritableOptions<T>` settings |
| Global hotkeys (`RegisterHotKey`) | Interface; Win32 implementation on Windows; hide the settings UI elsewhere |
| System tray (`NotifyIcon`) | Interface; Windows-only; decide what "minimize to tray" means on other targets |
| Toast notifications | An in-app `InfoBar` works on every target; keep OS toasts as a Windows implementation if they matter |
| Launch at login | Windows-only; hide elsewhere |
| Screen capture (`BitBlt`, `System.Drawing`) | Interface; Win32 capture on Windows; a file picker or clipboard image as the fallback |
| OCR (`Windows.Media.Ocr`) | Interface; Windows OCR on the WinAppSDK head; a cross-platform OCR library or cloud API elsewhere |
| Borderless, transparent, topmost windows | `AppWindow` presenters on desktop (`FullScreen`, `CompactOverlay`); full-screen pages elsewhere |
| COM interop | Rewrite with a cross-platform API, or Windows-only |
| Printing | Windows-only unless the app can export a PDF instead |

## Window management on desktop

```csharp
// The window the app created in OnLaunched
var appWindow = MainWindow.AppWindow;
appWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
// Restore when the page unloads
appWindow.SetPresenter(AppWindowPresenterKind.Default);
```

Check which `AppWindow` members each head implements before relying on one; fetch the docs for `AppWindow` on the target you ship.

## Gradual migration

When a big app cannot move in one step, run the WPF app and the Uno Platform app side by side from a shared library and move feature areas across. Uno Islands (hosting Uno Platform XAML inside a WPF window) is documented as an early preview built on the older Skia.Wpf head, with gaps in focus, keyboard, and drag-and-drop handling; confirm it works with the Uno Platform version in use before recommending it.

## Expectations from a real port

One open-source WPF utility (about 37,000 lines, OCR, screen capture, global hotkeys, tray) ported to about 13,000 lines covering roughly 75% of its features, verified end to end on Windows and WebAssembly. The gaps were all platform features: tray, launch at login, and parts of screen capture. Treat this as one data point: shell-integrated apps port their UI and logic well and keep a Windows-only tail.

## Common mistakes

- Treating `#if WINDOWS` as "running on Windows"; it misses the desktop head.
- Passing Windows-only types through service interfaces.
- Showing controls for features the current target cannot perform.
- Hard-coding target framework monikers in conditions; they change with each .NET release.
