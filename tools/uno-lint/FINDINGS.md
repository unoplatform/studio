# uno-lint: C# port of the Uno semantic linter, first findings

Date: 2026-09-30. Prototype state; see the spec in specs/001-uno-linter for the delivery plan.

## What was built

A netstandard2.0 rule library (`Uno.Lint.Core`) with a `dotnet tool`-style CLI on top (`uno-lint`).
All nine rules from the PowerShell prototype are ported, plus BINDVALUE. XAML was first read with `XDocument` (line info
preserved), C# with Roslyn syntax trees, so comments and strings can no longer trigger rules.

| Piece | State |
|---|---|
| Rule library, 10 rules + 4 info counters + suppression counts | done, 110 unit tests passing |
| CLI: path mode, `--json`, `--profile`, `--list-rules`, exit codes 0/1/2 | done |
| CLI: `--hook` (Claude Code PostToolUse payload on stdin, reports only edited lines) | done |
| Profiles: `recommended` (5 rules) / `strict` (9 rules) | done |
| `.editorconfig`: `dotnet_diagnostic.UNOL103.severity = none`, `uno_lint.profile = strict` | done |
| Toolkit gate: CARD/BACKBAR and the Toolkit BUILTIN suggestions turn off when the project has no Uno.Toolkit | done |
| MCP tool on `uno.devserver`, Roslyn analyzer package | not started |

Rule ids: UNOL001 HEX, UNOL002 TOKENTHEME, UNOL003 ICON, UNOL004 BUILTIN, UNOL005 RESPONSIVE, UNOL201 BINDVALUE
(recommended); UNOL101 CARD, UNOL102 BACKBAR, UNOL103 CODEBEHIND, UNOL104 OVERLAY (strict).

## Corpus

Five real apps, three of them agent-built (internal Studio samples). The prototype author's sample repos are private, so those
own apps are not in this run yet.

| App | Files | Toolkit | Who wrote it |
|---|---|---|---|
| uno.toolkit.ui Uno.Toolkit.Samples | 175 | yes | Uno team, gallery app |
| Uno.Themes ThemesSampleApp | 14 | no | Uno team |
| BrewHouse (coffee shop) | 28 | yes (SimpleTheme + Toolkit + MVUX) | agent |
| GridWatch (utility dashboard) | 28 | yes | agent |
| TestApp | 6 | no | agent, counter template |

## Results, strict profile

| Rule | Toolkit samples | Themes | BrewHouse | GridWatch | TestApp |
|---|---|---|---|---|---|
| HEX | 17 | 0 | 93 | 0 (18 merged into TOKENTHEME) | 0 |
| TOKENTHEME | 4 | 0 | 0 | 18 | 0 |
| ICON | 5 | 0 | 0 | 0 | 0 |
| BUILTIN | 0 | 0 | 0 | 0 | 0 |
| RESPONSIVE | 0 | 0 | 0 | 0 | 0 |
| CARD | 5 | off | 16 | 17 | off |
| BACKBAR | 0 | off | 0 | 0 | off |
| CODEBEHIND | 129 | 5 | 0 | 16 | 1 |
| OVERLAY | 0 | 0 | 0 | 0 | 0 |

The recommended profile on the same corpus reports only the first five rows.

## Differences from the PowerShell prototype

Every difference below is a false positive the port removes; the rest of the counts match exactly.

| Rule | PS | C# | Why |
|---|---|---|---|
| TOKENTHEME, Toolkit | 7 | 4 | `DividerBrush Color="{ThemeResource OnSurfaceColor}"` follows the theme; PS flagged it |
| CARD, Toolkit | 9 | 5 | swatch grid with `CornerRadius="8,0,0,0"`; a radius on some corners is not a card |
| RESPONSIVE, Toolkit | 1 | 0 | `Spacer.ActualHeight > 0` asks whether layout ran, not which breakpoint applies |
| HEX + TOKENTHEME, GridWatch | 18 + 18 | 0 + 18 | a brush defined once with a hex literal is one problem; reported once |
| Suppression comments | `<!-- xaml-lint: allow hex -->` was accepted | rejected | the `-->` terminator matched as the "reason"; a reason is now required for real |

Performance, same machine:

| Run | PowerShell | C# |
|---|---|---|
| Full scan, 175-file Toolkit samples | 2.8 s | 0.48 s |
| Hook, one edit | 460 to 490 ms | 127 to 135 ms |

## Per-rule verdict from reading the hits

**HEX (110 hits).** No false positives found. 93 of them are in BrewHouse, an app that has SimpleTheme, Toolkit and
MVUX enabled and still hard-codes a full brown palette on every page. This is exactly the failure the prototype was built to catch.
Keep in `recommended`.

**TOKENTHEME (22 hits).** After the ThemeResource fix, the survivors are brand brushes (Toolkit `UnoBlueColorBrush`)
and status brushes (GridWatch `StatusOnlineBrush`). All are correct by the rule, but the honest fix for a status
colour is often "rename it `*Invariant`", which is bureaucracy rather than a bug. Keep in `recommended`, consider
shipping it at `suggestion` severity.

**ICON (5 hits).** All inline Material Design Icons path data in one Toolkit sample page. Correct by the rule.
Keep in `recommended`.

**BUILTIN, RESPONSIVE, BACKBAR, OVERLAY (0 hits after calibration).** Nothing in this corpus exercises them, so
they are covered by unit tests only. Need more agent-built apps to calibrate.

**CARD (38 hits).** Two problems remain.
- Toolkit `OverviewPage` and `ShadowContainerSamplePage`: a `Border` with Background and CornerRadius inside a
  `utu:ShadowContainer` is the Toolkit's own documented pattern. The rule calls it a card.
- BrewHouse `CartPage` L71/L78: `CornerRadius="20" Padding="12,6"` with one word inside is a pill. It is a semantic
  bypass, but the fix is `utu:Chip`, not `CardContentControl`. Needs a chip heuristic.
Keep in `strict`.

**CODEBEHIND (151 hits).** Every hit is a real `object sender` handler. Whether that is a defect is an architecture
choice: on the Toolkit's own gallery app it flags 129 handlers, and an MVVM customer would see the same. Keep in
`strict`. In hook mode the tool defaults to `strict` so the agent sees the hint; the human decides what to keep via
`.editorconfig`.

## Open items

1. Chip/pill heuristic for CARD, and a ShadowContainer exemption or a better message.
2. ~~Dev-path skip regex~~: now matches whole folder names (`Tests/`, `MyApp.Tests/`), so `TestApp/`,
   `Content/TestPages/` and `ProbeResultsPage.xaml` are linted. Still worth making configurable.
3. Add the correctness rules from `uno-runtime-gotchas.md` that are bugs rather than taste: `x:Bind` against an
   MVUX-generated ViewModel, `{Binding State.Value}`, Uno0001 members read inside framework handlers. Those will earn
   the tool more trust than the style rules.
4. Get two or three more agent-built repos to calibrate BUILTIN, RESPONSIVE, BACKBAR, OVERLAY.
5. Expose the same library as an MCP tool on `uno.devserver --mcp-app`, then as a Roslyn analyzer.

## Review fixes (2026-10-01)

- Hook attribution reads `tool_response.structuredPatch` instead of searching for `new_string`, so a copied block,
  a short edit or unchanged context lines no longer land on the wrong lines.
- BUILTIN, the palette/icon file exemptions and the dev-path skip match whole words, not substrings.
- HEX fires only on color properties (`Text="#404"` is text).

Re-run on today's `uno.toolkit.ui` main (179 files, strict): identical to the pre-fix build except one HEX gained,
`SeedColorSamplePage.xaml` L25 `ColorPicker.Color="#5946D2"`, which the old palette-name match (`Color` in the file
name) had hidden. Themes sample unchanged. The agent-built apps were not re-run.

## Review fixes (2026-10-13)

Every confirmed bug from the PR review has a regression test in `tests/Uno.Lint.Tests/ReviewFixTests.cs`.

| Review point | Change |
|---|---|
| `.editorconfig` sections ignored | Section globs are honored per file with EditorConfig semantics (`[*.xaml]`, `[App/Tests/**]`, `[*.{xaml,cs}]`), nearest file and last matching section winning; severity is resolved per file |
| Suppression comment above a multi-line element did not reach an attribute hit | Attribute findings anchor on the element's start line as well as the attribute line |
| `CornerRadius="0"` reported as CARD | A uniform zero radius is a plain box |
| Avatars, dots and pills reported as CARD | Circle (equal explicit Width/Height, radius at least half), pill (radius at least half an explicit Height) and chip (radius 12+ around a single TextBlock or icon) are not cards |
| `<StaticResource x:Key="AliasBrush" ResourceKey="..."/>` reported as TOKENTHEME | Alias elements are skipped |
| Named colors and C# colors not covered | HEX now covers the WinUI named colors (except `Transparent`) in XAML and `Colors.X`, `Color.FromArgb`, `ColorHelper.FromArgb` in C# |
| Handler detection looser than FR-008 | Exactly two parameters, `object` first, type ending in `EventArgs` or `Args` second; the parameter-name heuristic is gone |
| JSON omitted the fix text | `fix` per finding; fix text adapts to the project (Uno.Themes role brushes vs WinUI brushes, Toolkit vs not) |
| Hook offered suppression on every hit | Hook output leads with the fix and never mentions suppression; allow comments the edit itself added are noted |
| Suppressions invisible | Counted per rule: `Supp` column in text, `suppressed` and `suppressedFindings` in JSON; suppressed hits of disabled rules are not counted |
| Hook walked every XAML file on each call | Project files are read first, XAML only for flags still unset, `bin`/`obj` are never entered, and the result is cached in the temp folder keyed by project-file timestamps |
| Roslyn 4.14 pin | Lowered to 4.8.0; `CSharpFile` accepts a compiler-provided `SyntaxTree` so an analyzer host never parses |
| No correctness rule in phase 1 | `BINDVALUE` (UNOL201): `{Binding State.Value}` outside a DataTemplate in MVUX projects, recommended profile |
| TOKENTHEME severity | Ships at `suggestion`; `.editorconfig` can raise it to `warning` |
| No CI for the tool | `.github/workflows/uno-lint.yml` builds, tests and packs on Ubuntu and Windows when `tools/uno-lint` changes |

`dnx` measured on a developer laptop against a local feed: 570 ms first use, 400 to 450 ms warm; the installed tool is
about 100 ms warm. The hook ships with the installed tool; `hooks/settings.hooks.dnx.json` is the zero-install variant.

### Corpus after the fixes, strict profile

uno.toolkit.ui has moved on since the first run (179 files; a FlexPanel playground sample was added, which is where the
extra CODEBEHIND and TOKENTHEME hits come from). The agent-built apps are unchanged.

| Rule | Toolkit samples | Themes | BrewHouse | GridWatch | TestApp |
|---|---|---|---|---|---|
| HEX | 288 (19 hex, 250 named, 19 in code) | 0 | 116 (93 hex, 23 named) | 31 | 0 |
| TOKENTHEME (suggestion) | 9 | 0 | 0 | 18 | 0 |
| ICON | 5 | 0 | 0 | 0 | 0 |
| BUILTIN | 0 | 0 | 0 | 0 | 0 |
| RESPONSIVE | 0 | 0 | 0 | 0 | 0 |
| BINDVALUE | off (no MVUX) | off | 0 | 0 | off |
| CARD | 4 | off | 11 | 16 | off |
| BACKBAR | 0 | off | 0 | 0 | off |
| CODEBEHIND | 145 | 5 | 0 | 16 | 1 |
| OVERLAY | 0 | 0 | 0 | 0 | 0 |
| Suppressed | 0 | 0 | 0 | 0 | 0 |

What moved and why:

- **HEX, named colors.** 250 named colors in the Toolkit gallery, 86 of them in one page (`ShadowContainerSamplePage`,
  `White`, `LightGray`, `Blue`, `Red` as demo fills). All correct by the rule; a gallery would set
  `[Samples/**] dotnet_diagnostic.UNOL001.severity = none`. BrewHouse gained 23, all `White` on backgrounds and button
  foregrounds, which is the same hard-coding as its hex palette. Named colors are the more common form of the problem
  in agent output than hex was.
- **HEX, in code.** 19 in the Toolkit samples: 11 `Color.FromArgb` and 8 `Colors.X`, in sample code-behind that builds
  demo brushes. GridWatch's 31 are hex on shapes plus `Colors.X` in converters.
- **CARD.** Toolkit 9 to 4 and BrewHouse 16 to 11 after the circle/pill/chip heuristics; the five BrewHouse drops are
  the `CornerRadius="20" Padding="12,6"` pills noted above. The ShadowContainer pattern still reports (open question).
- **CODEBEHIND.** Two `(object sender, object e)` methods in the Toolkit samples dropped out under the stricter
  signature; the FlexPanel playground added 18.
- **BINDVALUE.** Zero hits on the two MVUX apps: both bind states correctly. The rule is unit-tested only until an app
  with the trap turns up.
- **Suppressed.** Zero across the corpus: none of these apps carries allow comments yet. The column exists so that the
  first agent-built app that does is visible.

## Review fixes, round 2 (2026-10-13)

Every item from the second review, each with a regression test in `tests/Uno.Lint.Tests/ReviewRound2Tests.cs`.

| Review point | Change |
|---|---|
| Nested `.editorconfig` ignored in directory mode, honored in hook mode | Directory mode also loads every `.editorconfig` below the root, depth-ordered; single-file mode resolves the chain from the project root. Same file, same config, same result |
| `Directory.Build.props` above the csproj never read | Project files now include every `Directory.Build.props` / `Directory.Packages.props` from the drive root down to the project root, so the Toolkit, MVUX and Themes gates agree in every mode |
| HEX/TOKENTHEME fold keyed on line number | The fold is keyed on the element and goes the other way: the gating HEX finding is kept with a "also defined once for both themes" note; TOKENTHEME takes the brush only when HEX is not running on the file; `allow hex` suppresses it |
| Unkeyed children inside `ThemeDictionaries` flagged | Anything nested under a keyed resource inside `ThemeDictionaries` is exempt |
| `App.xaml.cs` lifecycle handlers flagged | `App.xaml.cs` and `Application` subclasses are exempt from CODEBEHIND |
| Preamble severities honored | Ignored and reported on stderr; put them under `[*]` |
| Suppression look-behind crossed element boundaries | It stops at the first non-comment line; consecutive comment lines still count; `allow all` documented |
| `panel` in the OVERLAY suffix set | Dropped; `RootPanel.Visibility` is CODEBEHIND |
| No palette-file exemption on the C# side | `Palette.cs`, `AppColors.cs`, `DesignTokens.cs` are exempt like their XAML counterparts |
| Path mode accepted any file; dev-path skip only in directory mode | Non `.xaml`/`.cs` files exit 2; a single file is linted relative to its project root so `Tests/` is skipped there too |
| XAML read twice in directory mode | Texts are read once and shared with context detection |
| "nine rules", BINDVALUE missing from FR-012, Key Entities | Fixed in the spec, README and here |
| Dead `Polyfills.cs`, 41 xUnit analyzer warnings | Removed; warnings fixed |

## Run it

```powershell
dotnet run --project src/Uno.Lint.Cli -c Release -- <path-to-app> --profile strict
dotnet run --project src/Uno.Lint.Cli -c Release -- <path-to-app> --json
dotnet test
```

## XAML rules moved onto Uno.XamlParser (2026-10-15)

All six XAML rules now run inside Uno.XamlParser as Stage-1 fixers; the XDocument reader is gone. Corpus findings are
identical except 6 false positives removed (`<SolidColorBrush x:Key="Red">` flagged on its key). Malformed agent
markup is linted instead of skipped (8 of 8 fixtures). Details, measurements and the issues found in the parser are
in [XAMLPARSER.md](XAMLPARSER.md).
