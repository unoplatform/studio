# New App

Defaults for a new app when the brief does not say otherwise. Explicit choices from the developer always win. Do not ask the developer to choose an architecture; these defaults are the answer unless they have already given a different one.

## Default stack

- **Presentation**: MVUX.
- **Navigation**: Uno.Extensions Navigation (routes, navigation data, dialog results as needed).
- **Theme**: follow the supplied design. With no design, the Simple theme with the template's Toolkit controls. See [design.md](design.md).
- **Targets**: Skia Desktop (Windows, Linux, macOS), WebAssembly, Android and iOS, using the template's target structure. Never narrow the targets to the workloads installed on this machine; report the ones you could not build or run instead.
- **Scope**: the client app. Integrate existing backend contracts. Propose missing backend work separately; do not create server projects or provision services unless asked.

## Scaffold

Check that the templates are installed:

```bash
dotnet new list unoapp
```

If `unoapp` is missing, the fix is `dotnet new install Uno.Templates`. Installing it changes the developer's machine, so follow the host's approval policy: run it if installs are allowed, otherwise give the developer the command. `uno-check` also reports missing templates and workloads.

Create the app:

```bash
dotnet new unoapp -preset recommended -presentation mvux -theme simple -tests unit -o <AppName>
```

- `<AppName>` is a valid C# identifier in PascalCase, derived from the brief.
- Pass `-theme` explicitly even though Simple is the preset default, so a change to the preset cannot change the app.
- `-tests unit` adds an NUnit test project. The recommended preset does not add one by default.
- Do not pass `-platforms`. The preset's target list is the default above.

Verified with Uno.Templates 6.7.30 (Uno.Sdk 6.7.30, .NET SDK 10.0.400): the command produces one app project targeting `net10.0-android`, `net10.0-ios`, `net10.0-browserwasm`, `net10.0-desktop` and plain `net10.0`, with the features `SimpleTheme`, `Hosting`, `Toolkit`, `Logging`, `MVUX`, `Configuration`, `HttpKiota`, `Serialization`, `Localization`, `Navigation`, `ThemeService` and `SkiaRenderer`, plus a test project using NUnit and FluentAssertions. The plain `net10.0` target is not a platform: `-tests unit` adds it so the test project can reference the app. Keep it. It builds for `net10.0-desktop` with no warnings and its sample test passes. On other template versions, check what was generated rather than assuming this list.

Start from these features. Add another `UnoFeatures` entry or package only when a concrete feature needs it, say why in the report, and follow the approval policy. Once the developer has approved a kind of addition, do not ask again for the same kind.

## Where things go

The template already creates `Presentation/`, `Models/` and `Services/`. One project is enough unless there is a stated complexity, reuse or testing need for more.

| Concern | Where | Rule |
|---|---|---|
| Views | `Presentation/<Name>Page.xaml` | Render state and forward actions. No storage, no network |
| Presentation state | `Presentation/<Name>Model.cs` | MVUX feeds, states and commands. Coordinates services. Load `uno-mvux-overview` first |
| Business rules | UI-independent classes, for example under `Models/` | No reference to views or `Microsoft.UI.Xaml` types |
| Storage, network, platform | Interfaces and implementations under `Services/`, registered in the host builder in `App.xaml.cs` | Injected into models |
| Routes | `RegisterRoutes` in `App.xaml.cs` | Load `uno-navigation-routes` and `uno-navigation-setup` |
| Settings the user changes | A configuration section written through `IWritableOptions<T>` from the `Configuration` feature | See the [writable configuration how-to](https://platform.uno/docs/articles/external/uno.extensions/doc/Learn/Walkthrough/WritableConfiguration.howto.html). A setting does not justify a database |

Add an interface, service or module only for a concrete benefit: isolation for tests, reuse, lifetime, a platform difference. Not for symmetry, and not one interface per class by convention.

## Blueprint

Before the first feature, fill in [../assets/blueprint.template.md](../assets/blueprint.template.md). Put it where the repository keeps design docs, or `docs/architecture.md` if there is no convention. It is plain Markdown, proportional to the app: a two-page app gets a short one. It has no approval gate; write it and continue. Keep it current.

The blueprint must name the owner and the observation and mutation path of every shared entity, session, preference and cross-page selection. Drafts and caches say how they commit, refresh or invalidate. Shared state does not imply a single global store.

## First feature

Choose a first journey that crosses the app's risky boundaries: persistence, a shared entity edited on two pages, an integration. Not the easiest static page. Implement it through view, state, rules, service, persistence and a test, build and exercise it, and only then repeat its pattern.

## Prototypes

If the developer asks for a prototype, scope narrows explicitly: say what is simulated. Persistence and integrations that are faked are still listed as gaps in the report.
