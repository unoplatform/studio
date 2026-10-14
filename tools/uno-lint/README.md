# uno-lint

Semantic lint for Uno Platform XAML and C#. It catches places where code, usually written by an AI agent,
hand-builds something the platform already provides, and names the control or resource to use instead.

Skills tell the agent what to do. This checks what it actually did, deterministically, in about a tenth of a second.

## Layout

```
src/Uno.Lint.Core   netstandard2.0 rule library (XDocument for XAML, Roslyn 4.8 for C#). The rules are host-agnostic:
                    they take a parsed document or SyntaxTree plus a ProjectContext. Linter.cs is the file-system host;
                    a Roslyn analyzer would be a second host over the same rules.
src/Uno.Lint.Xaml   experimental XAML host on Uno.XamlParser (net10.0), plugged in through IXamlEngine; see XAMLPARSER-SPIKE.md
src/Uno.Lint.Cli    uno-lint command: path mode, --json, --hook, --xaml-engine
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
| UNOL001 | HEX | recommended | hard-coded color on a color property outside a palette file: hex literal, named color (`Red`, `White`), or `Colors.X` / `Color.FromArgb` in C# | theme brush or a palette token (Uno.Themes role brushes when referenced, WinUI brushes otherwise) |
| UNOL002 | TOKENTHEME | recommended, `suggestion` | app brush with one value for both themes | role brush, ThemeDictionaries, or `{ThemeResource}` color |
| UNOL003 | ICON | recommended | inline path geometry or shapes drawn as an icon | SymbolIcon/FontIcon, or keyed data in Icons.xaml |
| UNOL004 | BUILTIN | recommended | custom control named like a platform/Toolkit control | RatingControl, ProgressBar, utu:Chip, utu:TabBar... |
| UNOL005 | RESPONSIVE | recommended | `ActualWidth`/`NewSize` comparisons in code-behind | `{utu:Responsive}` or AdaptiveTrigger |
| UNOL101 | CARD | strict, Toolkit only | rounded, filled or stroked `Border` | `utu:CardContentControl` |
| UNOL102 | BACKBAR | strict, Toolkit only | `Navigation.Request="-"` on a page with no NavigationBar | `utu:NavigationBar` MainCommand |
| UNOL103 | CODEBEHIND | strict | event handlers and `.Visibility =` in `*.xaml.cs` | commands, VisualStateManagerExtensions, regions |
| UNOL104 | OVERLAY | strict | `*Overlay/*Sheet/*Drawer.Visibility =` in code-behind | `Navigation.Request="!Route"` flyout |
| UNOL201 | BINDVALUE | recommended, MVUX only | `{Binding State.Value}` outside a DataTemplate; shows empty, writes nothing | `{Binding State, Mode=TwoWay}`; `{Binding Data.X}` inside FeedView |

Info counters (never gating): TOKEN, ADAPTIVE vs `utu:Responsive`, TEMPLATE, WORKAROUND. A `Supp` column counts
hits covered by an allow comment, per rule, so suppression is visible rather than a silent escape hatch.

Rules whose suggestion is a Toolkit control switch off automatically when the project references no Uno.Toolkit
(detected from `UnoFeatures`, `PackageReference` or the `using:Uno.Toolkit.UI` xmlns). Force it with `--toolkit` /
`--no-toolkit`. BINDVALUE switches on only when MVUX is referenced. Fix text adapts the same way: HEX names Uno.Themes
role brushes when Material, Cupertino or SimpleTheme is referenced, and WinUI theme brushes in a Fluent-only app.

CARD does not count a `Border` that is a circle (equal explicit Width/Height with radius at least half), a pill (radius
at least half an explicit Height), a chip (radius 12+ around a single TextBlock or icon), a zero radius, or a radius on
only some corners.

## Configuration

`.editorconfig`, same syntax and semantics as Roslyn analyzers, including section globs per path:

```ini
uno_lint.profile = strict                        # project-wide

[*]
dotnet_diagnostic.UNOL103.severity = none        # we are MVVM, handlers are fine
dotnet_diagnostic.UNOL002.severity = warning     # TOKENTHEME gates here (default is suggestion)

[Samples/**]
dotnet_diagnostic.UNOL001.severity = none        # demo pages may show literal colors
```

A severity key above any `[section]` is ignored (and reported), as a Roslyn analyzer would ignore it; nested
`.editorconfig` files below the lint root are loaded too, so `uno-lint .` and `uno-lint App/MainPage.xaml` agree.

One deliberate exception, reason required, in a comment directly above the hit (consecutive comment lines count, the
first non-comment line stops the search) or on the same line. For an attribute on a multi-line element the comment
goes above the element's start tag. `uno-lint: allow all - reason` covers every rule at that spot:

```xml
<!-- uno-lint: allow hex - chart series colour, no role covers it -->
<Border Background="#7A67F8" />
```

```csharp
// uno-lint: allow codebehind - MediaPlayerElement transport glue, no Toolkit equivalent
private void OnPositionChanged(object sender, RoutedEventArgs e) { }
```

The `xaml-lint:` prefix from the PowerShell prototype is accepted too. Every suppression is counted per rule in the
report and in `--json` (`suppressed`, `suppressedFindings`); keep them for framework failures with a recorded repro.

## Claude Code hook

Install the tool once, then merge `hooks/settings.hooks.json` into your Claude Code settings:

```bash
dotnet tool install -g Uno.Lint.Tool
```

The hook runs `uno-lint --hook` after every Edit or Write. It reads the tool payload on stdin, lints the saved file
with full context, reports only findings on the lines the edit added, defaults to the strict profile so the agent
sees every hint, leads with the fix (it does not offer suppression; any allow comments the edit added are noted and
counted), and always exits 0 so it never blocks an edit.

`hooks/settings.hooks.dnx.json` is the zero-install alternative using `dotnet dnx Uno.Lint.Tool`. Measured on a
developer laptop: installed tool about 100 ms per hook call, `dnx` about 400 to 450 ms warm and 570 ms on first use.
A hook that calls a missing `uno-lint` shows an error on every edit, so check `uno-lint --list-rules` works before
enabling it.

In single-file (hook) mode the project context (Toolkit, MVUX, Uno.Themes) is cached under the temp folder, keyed by
the project files' timestamps, so a hook call on a large solution does not re-read every XAML file; `bin` and `obj` are
never walked.

The added lines come from `tool_response.structuredPatch`, which Claude Code sends for Edit and Write. Hosts without
it fall back to diffing `old_string` against `new_string` and finding `new_string` in the file; when that text is
missing or appears more than once, the hook says nothing rather than guess.
