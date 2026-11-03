# uno-lint

Semantic lint for Uno Platform XAML and C#. It catches places where code, usually written by an AI agent,
hand-builds something the platform already provides, and names the control or resource to use instead.

Skills tell the agent what to do. This checks what it actually did, deterministically, right after each edit.

## Layout

```
src/Uno.Lint.Core    netstandard2.0, rules only: the C# rules (Roslyn 4.8), the rule catalog, the shared predicates
                     (RuleLogic), suppression comments and the immutable model. No file system, so a future Roslyn
                     analyzer package can ship it as is.
src/Uno.Lint.Xaml    net9.0 like Uno.XamlParser: the XAML rules as Stage-1 fixers, and UnoLintXaml, the three steps
                     any XamlParser host calls (AddTo, IsWellFormed, Collect). XamlLintEngine runs them for uno-lint.
src/Uno.Lint.Host    net10.0: the Linter (folder walk, project context and its cache, .editorconfig), the immutable
                     LintReport with its text rendering, and the agent-hook adapters (Claude Code, Codex).
src/Uno.Lint.Cli     the uno-lint command: lint, rules, hook, on Repl Toolkit.
tests/Uno.Lint.Tests xunit, grouped by subject; CliTests drive the command graph in memory, HookProcessTests run the
                     built tool with raw UTF-8 on stdin.
hooks/               the hook shim and settings snippet
FINDINGS.md          corpus results and per-rule verdicts
XAMLPARSER.md        how the XAML rules use the Uno.XamlParser package
```

## Build and run

```bash
dotnet build -c Release
dotnet test -c Release
```

```bash
dotnet run --project src/Uno.Lint.Cli -c Release -- lint path/to/app
dotnet run --project src/Uno.Lint.Cli -c Release -- lint path/to/app --profile strict --json
dotnet run --project src/Uno.Lint.Cli -c Release -- rules
dotnet run --project src/Uno.Lint.Cli -c Release -- lint --help
```

| Option of `lint` | Meaning |
|---|---|
| `[path]` | a project folder or one `.xaml`/`.cs` file; default the current folder |
| `--profile recommended\|strict` | rule set; default `uno_lint.profile` from `.editorconfig`, else `recommended` |
| `--top <n>` | files listed per rule in the text report (default 12) |
| `--include-dev` | also lint `Dev`, `Harness`, `Probe(s)` and `Test(s)` folders |
| `--toolkit` / `--no-toolkit` | force the Uno.Toolkit gate instead of detecting it |
| `--no-cache` | detect the project context again instead of reading the cached one |
| `--json`, `--output:yaml`... | structured output of the same report; the last format flag wins |
| `--markdown` | the text report in a Markdown code block |

Exit codes: `0` no finding at warning or above, `1` at least one, `2` the run could not complete (unknown or
malformed option, missing path, a file that is not `.xaml` or `.cs`).

To install as a global tool from a local build: `dotnet pack src/Uno.Lint.Cli -c Release -o artifacts` then
`dotnet tool install -g --add-source artifacts Uno.Lint.Tool`. The package is not on nuget.org yet.

## Rules

`uno-lint rules` prints this catalog; `uno-lint rules --json` gives it to scripts.

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
| UNOL201 | BINDVALUE | recommended, MVUX only, a bug | `{Binding State.Value}` outside a DataTemplate; shows empty, writes nothing | `{Binding State, Mode=TwoWay}`; `{Binding Data.X}` inside FeedView |

Info counters (never gating): TOKEN, ADAPTIVE vs `utu:Responsive`, TEMPLATE, WORKAROUND. A `Supp` column counts
hits covered by an allow comment, per rule, so suppression is visible rather than a silent escape hatch.

Rules whose suggestion is a Toolkit control switch off automatically when the project references no Uno.Toolkit
(detected from `UnoFeatures`, `PackageReference` or the `using:Uno.Toolkit.UI` xmlns). BINDVALUE switches on only
when MVUX is referenced. Fix text adapts the same way: HEX names Uno.Themes role brushes when Material, Cupertino or
SimpleTheme is referenced, and WinUI theme brushes in a Fluent-only app. A file outside any project (no `.csproj`,
`.sln` or `.slnx` above it) is linted with no references.

CARD does not count a `Border` that is a circle (equal explicit Width/Height with radius at least half), a pill (radius
at least half an explicit Height), a chip (radius 12+ around a single TextBlock or icon), a zero radius, or a radius on
only some corners.

## Configuration

`.editorconfig`, same syntax and semantics as Roslyn analyzers, including section globs per path:

```ini
uno_lint.profile = strict                        # this folder and below

[*]
dotnet_diagnostic.UNOL103.severity = none        # we are MVVM, handlers are fine
dotnet_diagnostic.UNOL002.severity = warning     # TOKENTHEME gates here (default is suggestion)

[Samples/**]
dotnet_diagnostic.UNOL001.severity = none        # demo pages may show literal colors

[Samples/Brand/**]
dotnet_diagnostic.UNOL001.severity = default     # back to the profile's severity here
```

Values: `default`, `none`, `silent`, `suggestion`, `warning`, `error`. A severity key above any `[section]` is ignored
(and reported), as a Roslyn analyzer would ignore it. Every file is governed by the `.editorconfig` files from its
folder up to the nearest `root = true`, wherever the run starts, so `uno-lint lint .` and
`uno-lint lint App/MainPage.xaml` agree. The same goes for the project: each file uses the nearest folder above it
that holds a project or solution.

One deliberate exception, reason required, in a comment directly above the hit (up to three comment lines, including
the lines of a multi-line comment; the first line that holds anything else stops the search) or on the same line. For
an attribute on a multi-line element the comment goes above the element's start tag. `uno-lint: allow all - reason` covers every rule at that spot:

```xml
<!-- uno-lint: allow hex - chart series colour, no role covers it -->
<Border Background="#7A67F8" />
```

```csharp
// uno-lint: allow codebehind - MediaPlayerElement transport glue, no Toolkit equivalent
private void OnPositionChanged(object sender, RoutedEventArgs e) { }
```

The `xaml-lint:` prefix from the PowerShell prototype is accepted too. Every suppression is counted per rule in the
report and in `--json` (`rules[].suppressed`, `suppressedFindings`); keep them for framework failures with a recorded
repro.

## Agent hook

`uno-lint hook` is a PostToolUse hook for Claude Code (Edit, MultiEdit, Write) and Codex (`apply_patch`). It reads the
payload on stdin as UTF-8, lints each edited file with full context, reports only findings on the lines the edit
added, bugs before patterns, defaults to the strict profile so the agent sees every hint, leads with the fix (it does
not offer suppression; allow comments the edit added are noted and counted), and always exits 0.

To wire it, install the tool and copy `hooks/uno-lint.sh` next to your hook settings; `hooks/hooks.json` is the
snippet, in the form the Studio plugin uses (`${CLAUDE_PLUGIN_ROOT}`). For a project's own `.claude/settings.json`,
point the command at the script's path instead. The shim exits quietly when `uno-lint` is not on PATH, so a machine
without the tool sees no hook error on every edit; set `UNO_LINT_CLI` to use another build.

How each harness's edit becomes added lines:

- **Claude Code**: `tool_response.structuredPatch` when present (every line for a Write that created the file);
  otherwise `new_string` is diffed against `old_string` and located in the saved file. When that text is missing or
  appears more than once, the hook says nothing rather than guess.
- **Codex**: the patch in `tool_input.command`, paths relative to the payload's `cwd`. Every line of an added file
  counts; each update hunk's new text is located in the saved file; deleted files are skipped.

Copilot CLI and Gemini CLI are not supported yet: their payloads and replies differ (`edit`/`create` tool names, a
top-level `additionalContext`, a timeout in milliseconds for Gemini).

Debugging: the hook writes nothing to stdout when there is nothing to say. When the payload is not JSON, an option is
unknown, or the lint fails, it writes one `uno-lint:` line to stderr and still exits 0; harnesses do not show an
exit-0 hook's stderr to the agent, so run it by hand to see it:

```bash
uno-lint hook < payload.json
```

In single-file runs the project context (Toolkit, MVUX, Uno.Themes) is cached per project, keyed by the project files'
paths and timestamps, in `uno-lint/context` under the user's local application data (`UNO_LINT_CACHE_DIR` overrides
the folder). `--no-cache` or `UNO_LINT_NO_CACHE=1` bypasses it. A reference first used only in XAML shows up after the
next project-file change.

Hook and scan times, with the machine and method, are in the spec's
[Performance](../../specs/001-uno-linter/spec.md#performance) section.
