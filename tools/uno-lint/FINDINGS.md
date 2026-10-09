# uno-lint: C# port of the Uno semantic linter, first findings

Date: 2026-09-30. Prototype state; see the spec in specs/001-uno-linter for the delivery plan.

## What was built

A netstandard2.0 rule library (`Uno.Lint.Core`) with a `dotnet tool`-style CLI on top (`uno-lint`).
All nine rules from the PowerShell prototype are ported. XAML is read with `XDocument` (line info
preserved), C# with Roslyn syntax trees, so comments and strings can no longer trigger rules.

| Piece | State |
|---|---|
| Rule library, 9 rules + 4 info counters | done, 78 unit tests passing |
| CLI: path mode, `--json`, `--profile`, `--list-rules`, exit codes 0/1/2 | done |
| CLI: `--hook` (Claude Code PostToolUse payload on stdin, reports only edited lines) | done |
| Profiles: `recommended` (5 rules) / `strict` (9 rules) | done |
| `.editorconfig`: `dotnet_diagnostic.UNOL103.severity = none`, `uno_lint.profile = strict` | done |
| Toolkit gate: CARD/BACKBAR and the Toolkit BUILTIN suggestions turn off when the project has no Uno.Toolkit | done |
| MCP tool on `uno.devserver`, Roslyn analyzer package | not started |

Rule ids: UNOL001 HEX, UNOL002 TOKENTHEME, UNOL003 ICON, UNOL004 BUILTIN, UNOL005 RESPONSIVE (recommended);
UNOL101 CARD, UNOL102 BACKBAR, UNOL103 CODEBEHIND, UNOL104 OVERLAY (strict).

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

## Run it

```powershell
dotnet run --project src/Uno.Lint.Cli -c Release -- <path-to-app> --profile strict
dotnet run --project src/Uno.Lint.Cli -c Release -- <path-to-app> --json
dotnet test
```
