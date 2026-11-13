# Before the Report

A new app that works can still look like the template it came from, and still fail without leaving a trace. Identity, artwork and the unhandled-exception handler are set when the app is created ([new-app.md](new-app.md)); this page has their detail. Go through the whole list once the requested journeys work, before filling in the report, and do then whatever was left. Each item ends as done, as a placeholder the report names, or as not run with its reason.

In an existing app this list does not apply: its identity, artwork and startup code are the developer's.

## App identity

The template writes placeholder values into the app's `.csproj`. Set them from the brief:

| Property | Set to |
|---|---|
| `ApplicationTitle` | The app's name as people read it, with its spaces |
| `ApplicationId` | The owner's reverse-domain identifier. The template's `com.companyname.<app>` is a placeholder |
| `ApplicationDisplayVersion`, `ApplicationVersion` | Leave `1.0` and `1` unless the brief gives a version |
| `ApplicationPublisher` | The owner named in the brief |
| `Description` | One sentence on what the app does, replacing the template's |

When the brief names no owner, keep the template's `ApplicationId` and `ApplicationPublisher`, and list both in the report as placeholders to replace before publishing. Do not invent an organisation. Changing `ApplicationId` after the app has run on a device installs it as a different app, so set it early.

## Icon and splash screen

The template ships Uno Platform artwork in `Assets/Icons/icon.svg`, `Assets/Icons/icon_foreground.svg` and `Assets/Splash/splash_screen.svg`.

- **Artwork was supplied**: replace the three files in place, keeping each file's name and canvas size.
- **No artwork**: replace the Uno logo with a plain mark, such as the app's initial on its primary colour, and list the icon and splash screen in the report as placeholders.
- Set the splash background to the app's own ground colour in the `.csproj` (`UnoSplashScreenBackgroundColor`, with `UnoSplashScreenDarkBackgroundColor` for dark). Uno Platform 7.0 removed the older `UnoSplashScreenColor`; use the property the project's resolved Uno.Sdk accepts, and build to confirm.

These files and properties are packaged at build time, so they need a build and a start, not Hot Reload.

## Template leftovers

Search the app project for what the template or your own work left behind, and remove it:

- sample pages, models, services and endpoints the app no longer navigates to or calls;
- sample text still on screen or in resource files, and the template's own README text;
- commented-out blocks, unused `using` lines and files nothing references.

Remove files and text only. Every `UnoFeatures` entry, the host builder, the service registrations and `RegisterRoutes` stay, as [new-app.md](new-app.md) says.

## Failures leave a trace

- **Unhandled exceptions reach the app's log.** Uno Platform generates a handler for `Application.UnhandledException` that writes to the debugger output only, which is nothing once the app runs outside a debugger. Subscribe once, in the `App` constructor, and log through the app's logger:

  ```csharp
  UnhandledException += (_, e) =>
      Host?.Services.GetService<ILogger<App>>()?.LogError(e.Exception, "Unhandled exception: {Message}", e.Message);
  ```

  The handler does nothing that can throw, shows no UI and does not set `Handled` to hide the failure.
- **An `async void` method catches.** An exception that leaves an event handler or `OnLaunched` declared `async void` has no caller to return to. Its body awaits inside `try`/`catch`, logs, and leaves the page in a visible error state. Prefer an MVUX command, which a `FeedView` or the command's own error path already covers, to an event handler.
- **No empty `catch`.** A caught exception is logged or shown; one that is expected says why in a comment.

## JSON the app reads or writes itself

Files, cached responses and navigation payloads the app serialises go through `ISerializer<T>` from the `Serialization` feature, with a source-generated context:

```csharp
[JsonSerializable(typeof(Settings))]
internal partial class AppJsonContext : JsonSerializerContext { }
```

Register each type in the host builder, then inject `ISerializer<Settings>` where it is used:

```csharp
.UseSerialization(services => services
    .AddJsonTypeInfo(AppJsonContext.Default.Settings))
```

Reflection-based `JsonSerializer.Serialize(value)` works in Debug and can fail in a trimmed Release build, which is how the app ships. A client that Kiota generated serialises its own models and needs none of this.

## Nothing dropped

Fill rule 16's grid from the code, not from memory: for each table that has a page, find the delete command, the unsaved-changes check, the related lists, the picker behind each lookup field, the four list templates and the sort. An empty cell is built now, ruled out by the brief or the backend with the reason, or listed under *Implementation gaps*.

## Release build

Build `-c Release` for every target that can build on this machine, per *Release build* in [verification.md](verification.md), and start one of them: trimming and resource packaging show their failures only there. Record the targets that could not build or start.

## In the report

The report's checks table carries a row for the Release build, one for this list and one for the capability grid. Placeholders (identifier, publisher, icon, splash) go under *Decisions and deviations*, so the developer sees what to replace.
