# uno-lint: corpus results

What the rules report on real apps, and what reading every hit says about each rule. The spec in
[specs/001-uno-linter](../../specs/001-uno-linter/spec.md) has the delivery plan; this file is the evidence behind the
rule tiers.

## Corpus

| App | Revision | Files | References | Who wrote it |
|---|---|---|---|---|
| [uno.toolkit.ui](https://github.com/unoplatform/uno.toolkit.ui) `samples/` | `c4c8f887` | 203 | Toolkit, Uno.Themes | Uno team, gallery app |
| [Uno.Themes](https://github.com/unoplatform/Uno.Themes) `src/samples/` | `27fb1a61` | 192 | Uno.Themes | Uno team |
| Coffee-shop sample | internal | 28 | SimpleTheme, Toolkit, MVUX | agent |
| Utility-dashboard sample | internal | 28 | Toolkit, MVUX | agent |
| Counter template | internal | 6 | none | agent |

The two public apps can be re-run by anyone at the revisions above. The three agent-built Studio samples are not
public, so only conclusions are given for them.

## Results, strict profile

```bash
uno-lint lint <uno.toolkit.ui>/samples --profile strict
uno-lint lint <Uno.Themes>/src/samples --profile strict
```

| Rule | Toolkit samples | Uno.Themes samples |
|---|---|---|
| HEX | 287 | 23 |
| TOKENTHEME (suggestion) | 4 | 4 |
| ICON | 5 | 46 |
| BUILTIN | 0 | 0 |
| RESPONSIVE | 0 | 0 |
| CARD | 4 | off (no Toolkit) |
| BACKBAR | 0 | off (no Toolkit) |
| CODEBEHIND | 145 | 30 |
| OVERLAY | 0 | 0 |
| BINDVALUE | off (no MVUX) | off (no MVUX) |
| Suppressed | 0 | 0 |

The recommended profile reports HEX, TOKENTHEME, ICON, BUILTIN, RESPONSIVE and BINDVALUE only.

Every finding on the five apps (strict profile) is identical between this build and the one before the 2026-10-19
restructure: same rule, file, line and message.

## Per-rule verdict from reading the hits

**HEX.** No false positives found. Most Toolkit hits are named colors (`White`, `LightGray`, `Red`) used as demo fills
in gallery pages, plus `Color.FromArgb` and `Colors.X` in sample code-behind; a gallery would set
`[Samples/**] dotnet_diagnostic.UNOL001.severity = none`. In the agent-built coffee-shop sample, which has SimpleTheme,
Toolkit and MVUX enabled, HEX is by far the largest count: the whole palette is hard-coded on every page, in hex and as
named colors. That is the failure the linter exists for. Keep in `recommended`.

**TOKENTHEME.** The survivors are brand and status brushes defined once. Correct by the rule, but the honest fix for a
status color is often a rename to `*Invariant`. Ships at `suggestion`; `.editorconfig` can raise it to `warning`.

**ICON.** Inline icon path data in sample pages. Correct by the rule. Keep in `recommended`.

**CARD.** The circle, pill and chip heuristics removed the avatars and pills the first runs reported. One problem
remains: a `Border` with Background and CornerRadius inside `utu:ShadowContainer` is the Toolkit's own documented
pattern, and CARD still calls it a card (open question in the spec). Keep in `strict`.

**CODEBEHIND.** Every hit is a real `object sender` handler. Whether that is a defect is an architecture choice: on the
Toolkit's own gallery it flags well over a hundred handlers, and an MVVM customer would see the same. Keep in `strict`.
In hook mode the tool defaults to `strict` so the agent sees the hint; the human decides what to keep via
`.editorconfig`.

**BINDVALUE.** No hits: the two MVUX samples bind states correctly. Unit-tested only until an app with the trap turns
up.

**BUILTIN, RESPONSIVE, BACKBAR, OVERLAY.** No hits on this corpus, so they are covered by unit tests only. More
agent-built apps are needed to calibrate them.

## Differences from the PowerShell prototype

Every difference is a false positive the C# tool removes; on the apps both were run on, the other counts matched.

| Rule | Cause |
|---|---|
| TOKENTHEME | `DividerBrush Color="{ThemeResource OnSurfaceColor}"` follows the theme |
| CARD | a swatch grid with `CornerRadius="8,0,0,0"`; a radius on some corners is not a card |
| RESPONSIVE | `Spacer.ActualHeight > 0` asks whether layout ran, not which breakpoint applies |
| HEX + TOKENTHEME | a brush defined once with a literal color is one problem; reported once, under HEX |
| Suppression | `<!-- xaml-lint: allow hex -->` was accepted because the `-->` terminator matched as the reason |

## Open items

1. ShadowContainer: exempt the documented pattern from CARD, or point the message at `ElevatedCardContentControlStyle`.
2. Correctness rules from the runtime gotchas: `x:Bind` against an MVUX-generated ViewModel, Uno0001 members read
   inside framework handlers. They will earn the tool more trust than the style rules.
3. Two or three more agent-built repositories to calibrate BUILTIN, RESPONSIVE, BACKBAR and OVERLAY.
4. The MCP tool on `uno.devserver --mcp-app`, then the Roslyn analyzer.
