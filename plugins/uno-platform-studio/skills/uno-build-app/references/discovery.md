# Discovery

Read representative files, not the whole tree. For each dimension below, record what is **declared** and what is **resolved**. They differ more often than you would expect, and the difference is where defects hide.

## What to read first

1. The request, and any brief, design files or assets the developer supplied.
2. The project's agent instructions: `AGENTS.md`, `CLAUDE.md`, `.github/copilot-instructions.md`, and any rules folder the repository uses. These outrank this skill's defaults.
3. The README, then one or two representative pages, models and services in the area the request touches.
4. The working tree: `git status`. Preserve uncommitted changes you did not make, and never reset or discard them.

## Dimensions

| Dimension | Declared | Resolved | Notes |
|---|---|---|---|
| Uno SDK | `global.json` → `msbuild-sdks` → `Uno.Sdk`, or a version on the project's SDK declaration | Evaluated `UnoSdkVersion` or the SDK import path in diagnostic build output | `obj/project.assets.json` records library packages, not the MSBuild SDK resolution |
| .NET SDK | `global.json` → `sdk` | `dotnet --version` in the project directory | |
| Targets | `<TargetFrameworks>` in the app `.csproj` | Which of those can build *and run* on this machine | Declared is not runnable. Never narrow the declared list to match the machine |
| Features | `<UnoFeatures>` in the app `.csproj` and imported props | Evaluated features and resolved package references in `obj/project.assets.json` | Packages may also be referenced explicitly or supplied by another feature; inspect resolution before deciding an API is unavailable. Adding a feature is a dependency change |
| Presentation | `MVUX` or `MVVM` in `UnoFeatures`, models under `Presentation/` | How each page in scope actually binds | A project flag is not proof every page follows it. Mixed is a valid answer |
| Navigation | `Navigation` feature, `RegisterRoutes` in `App.xaml.cs` | How the pages in scope actually navigate | Some apps mix Uno.Extensions Navigation with `Frame` or code-behind. Follow the local pattern |
| Theme | Theme element in `App.xaml` or its merged dictionaries | Which theme family, which customisation mechanism | Preserve legacy setups. See [design.md](design.md) |
| Services | `Services/`, host builder registration in `App.xaml.cs` | Who owns each piece of state and storage today | Reuse existing ownership boundaries |
| Tests | Test projects in the solution | Which pass today, which behaviours have no test | Say honestly what is not covered |
| Capabilities | MCP servers configured for the agent | What this session can actually call | See below |

Useful commands:

```bash
dotnet --version
dotnet msbuild <App>.csproj -getProperty:TargetFrameworks
dotnet msbuild <App>.csproj -p:TargetFramework=<declared-tfm> -getProperty:UnoSdkVersion,UnoFeatures
dotnet list <App>.csproj package
```

Ground unfamiliar or version-sensitive APIs in official documentation and the resolved package's source or metadata. When the Uno docs MCP is available, search first, then fetch using the result's `sourcePath` and optional `anchor`; otherwise use official web documentation or source. Current docs may describe a newer release than the app uses.

## Capabilities: four different states

For each tool you intend to rely on (Uno App MCP, the Uno docs MCP, an emulator, a browser), distinguish:

- **Missing**: not installed or not registered.
- **Disabled**: registered but turned off for this session.
- **Not configured**: present but lacking something it needs, such as an app to attach to.
- **Not running**: configured, but its process or target is not up.

Report the state you observed, not a guess. The Uno App MCP reports its own state through `uno_health`: a degraded or "discovering" state with no solution selected means no app is attached yet, not that the tool is broken.

## Baseline before you change anything

Before the first edit, build the targets in scope and run the existing tests once. Record what already fails. Later, a failure that existed before your change is attributed to the baseline, with that evidence, and reported separately from anything your change broke. Do not repair unrelated failures. If you cannot tell which it is, say the attribution is uncertain.

## Secrets

Never copy connection strings, tokens, keys or personal data from the project into reports, checkpoints or handoff notes.
