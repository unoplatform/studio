# uno-lint

Semantic lint for Uno Platform XAML and C#. It catches places where code, usually written by an AI agent,
hand-builds something the platform already provides, and names the control or resource to use instead.

Skills tell the agent what to do. This checks what it actually did, deterministically, in about a tenth of a second.

## Layout

```
src/Uno.Lint.Core   netstandard2.0 rule library (XDocument for XAML, Roslyn for C#); future home of the analyzer
src/Uno.Lint.Cli    uno-lint command: path mode, --json, --hook
tests/Uno.Lint.Tests xunit
hooks/              Claude Code PostToolUse hook snippet
FINDINGS.md         results of the first corpus run and per-rule verdicts
```

## Build and run

```bash
dotnet build -c Release
dotnet test
```

```bash
dotnet run --project src/Uno.Lint.Cli -c Release -- path/to/app
dotnet run --project src/Uno.Lint.Cli -c Release -- path/to/app --profile strict --json
dotnet run --project src/Uno.Lint.Cli -c Release -- --list-rules
```

Exit codes: `0` clean, `1` findings at warning or above, `2` bad input.

To install as a global tool locally: `dotnet pack src/Uno.Lint.Cli -c Release` then
`dotnet tool install -g --add-source src/Uno.Lint.Cli/bin/Release Uno.Lint.Tool`.

## Rules

| Id | Name | Profile | Catches | Use instead |
|---|---|---|---|---|
| UNOL001 | HEX | recommended | hex color literal on a color property outside a palette file | theme role brush or a palette token |
| UNOL002 | TOKENTHEME | recommended | app brush with one value for both themes | role brush, ThemeDictionaries, or `{ThemeResource}` color |
| UNOL003 | ICON | recommended | inline path geometry or shapes drawn as an icon | SymbolIcon/FontIcon, or keyed data in Icons.xaml |
| UNOL004 | BUILTIN | recommended | custom control named like a platform/Toolkit control | RatingControl, ProgressBar, utu:Chip, utu:TabBar... |
| UNOL005 | RESPONSIVE | recommended | `ActualWidth`/`NewSize` comparisons in code-behind | `{utu:Responsive}` or AdaptiveTrigger |
| UNOL101 | CARD | strict, Toolkit only | rounded, filled or stroked `Border` | `utu:CardContentControl` |
| UNOL102 | BACKBAR | strict, Toolkit only | `Navigation.Request="-"` on a page with no NavigationBar | `utu:NavigationBar` MainCommand |
| UNOL103 | CODEBEHIND | strict | event handlers and `.Visibility =` in `*.xaml.cs` | commands, VisualStateManagerExtensions, regions |
| UNOL104 | OVERLAY | strict | `*Overlay/*Sheet/*Drawer.Visibility =` in code-behind | `Navigation.Request="!Route"` flyout |

Info counters (never gating): TOKEN, ADAPTIVE vs `utu:Responsive`, TEMPLATE, WORKAROUND.

Rules whose suggestion is a Toolkit control switch off automatically when the project references no Uno.Toolkit
(detected from `UnoFeatures`, `PackageReference` or the `using:Uno.Toolkit.UI` xmlns). Force it with `--toolkit` /
`--no-toolkit`.

## Configuration

`.editorconfig`, same syntax as Roslyn analyzers:

```ini
uno_lint.profile = strict
dotnet_diagnostic.UNOL103.severity = none        # we are MVVM, handlers are fine
dotnet_diagnostic.UNOL002.severity = suggestion
```

One deliberate exception, reason required, in a comment within three lines above the hit or on the same line:

```xml
<!-- uno-lint: allow hex - chart series colour, no role covers it -->
<Border Background="#7A67F8" />
```

```csharp
// uno-lint: allow codebehind - MediaPlayerElement transport glue, no Toolkit equivalent
private void OnPositionChanged(object sender, RoutedEventArgs e) { }
```

The `xaml-lint:` prefix from the PowerShell prototype is accepted too.

## Claude Code hook

`hooks/settings.hooks.json` runs `uno-lint --hook` after every Edit or Write. The hook reads the tool payload on
stdin, lints the saved file with full context, and reports only findings on the lines the edit added, defaults to
the strict profile so the agent sees every hint, and always exits 0 so it never blocks an edit.

The added lines come from `tool_response.structuredPatch`, which Claude Code sends for Edit and Write. Hosts without
it fall back to diffing `old_string` against `new_string` and finding `new_string` in the file; when that text is
missing or appears more than once, the hook says nothing rather than guess.
