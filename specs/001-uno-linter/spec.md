# Feature Specification: Uno Linter

**Feature Branch**: `001-uno-linter`  
**Created**: 2026-10-01  
**Status**: Draft  
**Input**: User description: "A deterministic linter for Uno Platform XAML and C# that verifies what an AI agent actually wrote, complementing the skills that tell it what to write. Ships as one rule library exposed through the Studio plugin hook, an MCP tool on the Uno dev server, and later a Roslyn analyzer."

## Summary

Skills advise, but nothing verifies. The `uno-platform-studio` plugin ships skills that tell an agent how to use MVUX, navigation, the Toolkit and theming, and agents still drift: context fills up, a skill is not invoked, or the model reasons its way back to a shortcut. Every sample built with the plugin so far came back with hard-coded hex colors, cards drawn as rounded Borders and icons drawn from shapes, despite skills saying otherwise.

The Uno Linter closes the loop. It reads XAML with a real XML parser and C# with Roslyn, applies a small set of semantic rules, and reports each hit as file, line, rule id, what was found and what to use instead. It runs in about a tenth of a second after each edit so the agent fixes the problem in the same turn, never blocks an edit, and only reports the lines the edit added.

A working C# prototype exists under [`tools/uno-lint`](../../tools/uno-lint/) with 32 passing tests and a first run across five real apps. The decisions this spec asks for are the rule tiers, the first shipping surface and who owns the rule list.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Agent gets corrected in the same turn (Priority: P1)

A developer is building an Uno app with the Studio plugin in Claude Code, Copilot or Codex. The agent edits `CartPage.xaml` and adds a `Border` with `Background="#FFF5EFE6"` and `CornerRadius="16"`. Immediately after the edit, the linter runs on the saved file and hands the agent two findings: a hex literal where a theme brush belongs, and a Border drawn as a card where `utu:CardContentControl` belongs. The agent fixes both before moving to the next file. The developer never sees the shortcut.

**Why this priority**: This is the whole reason the product exists. Catching the pattern at the first edit stops it from being copied to every later screen, which is how one workaround becomes the app's architecture.

**Independent Test**: Can be fully tested by installing the plugin hook, asking an agent to add a colored rounded Border to a page, and verifying the agent receives the two findings as context and rewrites the element. Delivers value without any other surface.

**Acceptance Scenarios**:

1. **Given** the plugin hook is installed and an agent edits a `.xaml` or `.cs` file, **When** the edit adds a pattern a rule covers, **Then** the agent receives the findings for that edit as additional context within the same tool turn
2. **Given** an edit adds a hex literal and a hand-drawn card on the same line, **When** the hook runs, **Then** both findings are reported with the line number, the rule id and the replacement to use
3. **Given** an edit touches a file that already contains 40 older findings, **When** the hook runs, **Then** only findings inside the lines the edit added are reported
4. **Given** the linter finds problems, **When** the hook returns, **Then** the edit is never rejected or rolled back and the hook exits successfully
5. **Given** a file fails to parse or the linter itself throws, **When** the hook runs, **Then** the agent sees nothing and the edit proceeds unchanged

---

### User Story 2 - Team turns off rules that do not fit its architecture (Priority: P1)

A team builds MVVM apps with event handlers in code-behind and no Uno.Toolkit. They run the linter and see hundreds of CODEBEHIND hits and suggestions to use Toolkit controls they do not reference. They set one line in `.editorconfig` to turn CODEBEHIND off, and the Toolkit-only rules were already off because the project has no Toolkit reference. The remaining findings are ones they agree with.

**Why this priority**: Without this, half the rules produce noise for anyone not on MVUX + Extensions navigation + Toolkit, and the tool gets disabled. The Toolkit sample app alone produced 129 CODEBEHIND hits.

**Independent Test**: Can be fully tested by running the linter on an app without Toolkit in strict mode and verifying CARD and BACKBAR report as off, then adding `dotnet_diagnostic.UNOL103.severity = none` and verifying CODEBEHIND disappears.

**Acceptance Scenarios**:

1. **Given** a project with no Uno.Toolkit reference in `UnoFeatures`, `PackageReference` or XAML namespaces, **When** the linter runs in any profile, **Then** CARD, BACKBAR and Toolkit-specific BUILTIN suggestions are disabled and reported as off
2. **Given** a `.editorconfig` with `dotnet_diagnostic.UNOL103.severity = none`, **When** the linter runs, **Then** CODEBEHIND findings are not reported
3. **Given** a `.editorconfig` with `uno_lint.profile = strict`, **When** the linter runs without a `--profile` switch, **Then** all nine rules are evaluated
4. **Given** no configuration, **When** the linter runs from the command line, **Then** the recommended profile applies and only the five universal rules report
5. **Given** a deliberate exception with a reason comment within three lines above the hit, **When** the linter runs, **Then** that hit is suppressed; **Given** the same comment without a reason, **Then** the hit is still reported

---

### User Story 3 - Maintainer audits an existing app and tracks counts (Priority: P2)

A sample-gallery maintainer runs the linter on a whole project before publishing. They get a count per rule and the top files for each, fix or suppress each hit, and the run exits clean. Over time they compare counts across apps to see which skills or templates still produce shortcuts.

**Why this priority**: Counts make the problem measurable. They tell us whether a new skill version reduced hard-coded colors, and they give CI a gate.

**Independent Test**: Can be fully tested by running `uno-lint <path>` on an app, verifying the per-rule table and exit code 1, fixing the hits, and verifying exit code 0.

**Acceptance Scenarios**:

1. **Given** a project folder, **When** the linter runs in path mode, **Then** it prints a count per rule, the top files per rule with sample findings, and the four info counters
2. **Given** findings at warning severity exist, **When** the run ends, **Then** the exit code is 1; **Given** none exist, **Then** it is 0; **Given** the path does not exist, **Then** it is 2
3. **Given** the `--json` switch, **When** the run ends, **Then** output is machine-readable with root, profile, Toolkit detection, counts, info and one entry per finding
4. **Given** files under `bin`, `obj`, `.git`, or generated `*.g.cs`, **When** the linter runs, **Then** they are skipped; **Given** Dev, Harness, Probe or Tests path segments, **Then** they are skipped unless `--include-dev` is passed

---

### User Story 4 - Agent calls the linter on demand through MCP (Priority: P2)

An agent in a host without a hook system, or a skill that says "verify before you report done", calls a `lint_project` or `lint_file` tool on the Uno MCP server that Studio users already have through `uno.devserver`. It receives the same JSON the CLI produces.

**Why this priority**: The MCP tool reaches every agent that already has the Uno MCP, including ones outside the three plugin hosts, and lets skills make verification a step rather than a hope.

**Independent Test**: Can be fully tested by calling the MCP tool on the Toolkit sample app and verifying its counts match the CLI run on the same path.

**Acceptance Scenarios**:

1. **Given** the Uno MCP server is running, **When** an agent calls the lint tool with a project path, **Then** it receives the same findings and counts the CLI reports for that path
2. **Given** an agent calls the tool with a single file, **When** the tool runs, **Then** project context (Toolkit detection, `.editorconfig`) is resolved from the nearest `.csproj` and applied

---

### User Story 5 - Developer sees warnings in the IDE (Priority: P3)

A developer opening the project in Visual Studio, Rider or running `dotnet build` sees the same findings as compiler warnings with the same ids, configured by the same `.editorconfig` lines.

**Why this priority**: It extends the benefit to humans writing code by hand and makes the rules part of the normal .NET toolchain. It depends on the rule list being stable first.

**Independent Test**: Can be fully tested by referencing the analyzer package in an app and verifying a hard-coded hex literal produces warning UNOL001 at build time.

**Acceptance Scenarios**:

1. **Given** the analyzer package is referenced, **When** the project builds, **Then** each finding appears as a diagnostic with its UNOL id, file and line
2. **Given** a `.editorconfig` severity override, **When** the project builds, **Then** the analyzer honors it exactly as the CLI does

---

### Edge Cases

- A XAML file that does not parse as XML is counted as unparsed and skipped; the run continues and the count is reported.
- An edit whose text cannot be located in the saved file (concurrent change, different path) produces no findings rather than blaming the agent for the whole file.
- Line endings differ between the edit payload and the saved file (CRLF vs LF); the edit is still located.
- A brush whose `Color` is a `{ThemeResource}` follows the theme and is not a TOKENTHEME finding even though it is defined once.
- A `Border` with a radius on only some corners (`8,0,0,0`) is a swatch or joined edge, not a card.
- `ActualHeight > 0` asks whether layout has run; it is not a breakpoint.
- A brush defined once with a hex literal is one problem; it is reported once as TOKENTHEME, not also as HEX.
- A suppression comment with no reason (`allow hex -->`) does not suppress; the comment terminator is not a reason.
- A project named `TestApp` must not be skipped by the dev-path rule, which matches only `/Tests/` as a path segment.

## Requirements *(mandatory)*

### Functional Requirements

#### Rules

- **FR-001**: System MUST detect hex color literals in XAML attributes and element content outside palette files (`*Palette*.xaml`, `*Colors*.xaml`, `*Tokens*.xaml`) and theme override files (`ColorPaletteOverride*.xaml`); keyed resources inside `ResourceDictionary.ThemeDictionaries` are exempt (HEX, UNOL001)
- **FR-002**: System MUST detect app-defined `*Brush` resources outside `ThemeDictionaries` whose color is fixed, excluding keys named `*Invariant`, `*OnDark`, `*OnLight` or `*Fixed` and excluding brushes whose color is a `{ThemeResource}` (TOKENTHEME, UNOL002)
- **FR-003**: System MUST detect inline path-mini-language `Data` on `Path`/`PathIcon`, inline `PathGeometry`/`GeometryGroup`, and basic shapes composed inside a `Viewbox`, outside `Icons*.xaml` files and outside `ControlTemplate` elements (ICON, UNOL003)
- **FR-004**: System MUST detect classes deriving from a control base type whose name matches a platform or Toolkit control and whose companion XAML does not already use that control (BUILTIN, UNOL004)
- **FR-005**: System MUST detect relational comparisons against `ActualWidth`, `ActualHeight`, `NewSize` or `Bounds` in `*.xaml.cs`, excluding comparisons against zero (RESPONSIVE, UNOL005)
- **FR-006**: System MUST detect `Border` elements with a uniform `CornerRadius` and a `Background`, `BorderBrush` or `Padding`, outside `ControlTemplate` elements, treating a Border without Background inside `ShadowContainer` as not a card (CARD, UNOL101)
- **FR-007**: System MUST detect `Navigation.Request="-"` in a XAML file that contains no `NavigationBar` (BACKBAR, UNOL102)
- **FR-008**: System MUST detect event-handler methods (`object sender` plus an `EventArgs` parameter) and `.Visibility =` assignments in `*.xaml.cs` (CODEBEHIND, UNOL103)
- **FR-009**: System MUST detect `.Visibility =` assignments whose target name ends in Overlay, Sheet, Scrim, Drawer, Popup or Panel and report them as OVERLAY (UNOL104) instead of CODEBEHIND
- **FR-010**: System MUST report four non-gating info counters: references to app-defined brush keys, AdaptiveTrigger count against `utu:Responsive` count, app-authored ControlTemplate count, and `WORKAROUND(` marker count
- **FR-011**: Each finding MUST carry the rule id, rule name, file path relative to the lint root, 1-based line, a message naming what was found, and the fix text naming what to use instead

#### Profiles and configuration

- **FR-012**: System MUST provide a `recommended` profile enabling HEX, TOKENTHEME, ICON, BUILTIN and RESPONSIVE, and a `strict` profile enabling all nine rules
- **FR-013**: System MUST default to `recommended` in path mode and to `strict` in hook mode unless a profile is given explicitly
- **FR-014**: System MUST read `dotnet_diagnostic.UNOLnnn.severity = none|suggestion|warning|error` and `uno_lint.profile = recommended|strict` from `.editorconfig` files from the lint root upward, stopping at `root = true`, nearest file winning
- **FR-015**: System MUST disable rules whose fix is a Toolkit control when the project has no Uno.Toolkit reference, detected from `UnoFeatures`, `PackageReference` or the `using:Uno.Toolkit.UI` xmlns, with `--toolkit` and `--no-toolkit` overrides
- **FR-016**: System MUST suppress a single finding when a comment within the three non-blank lines above it, or on the same line, reads `uno-lint: allow <rule> - <reason>`, and MUST NOT suppress when the reason is absent; the legacy `xaml-lint:` prefix MUST be accepted

#### Surfaces

- **FR-017**: System MUST provide a command-line entry point taking a folder or file path with `--profile`, `--json`, `--top`, `--include-dev`, `--list-rules` switches, exiting 0 when clean, 1 when findings at warning or above remain, 2 on bad input
- **FR-018**: System MUST provide a `--hook` mode that reads the Claude Code PostToolUse payload on standard input, lints the saved file for full context, keeps only findings inside the lines the edit added, returns them as `additionalContext`, and always exits 0
- **FR-019**: The hook MUST locate the edited text in the saved file regardless of CRLF/LF differences and re-indentation, and MUST report nothing when the text cannot be located
- **FR-020**: System MUST expose the same rules as an MCP tool on the Uno dev server returning the same JSON shape as the CLI (phase 2)
- **FR-021**: System MUST expose the same rules as a Roslyn analyzer package honoring the same `.editorconfig` keys (phase 3)
- **FR-022**: The rule library MUST target netstandard2.0 so one assembly can serve the CLI, the MCP tool and the analyzer

### UX Requirements *(mandatory for UI features)*

This feature has no graphical UI. Its user-facing surface is text consumed by agents and developers.

- **UX-001**: Every finding returned to an agent MUST name the replacement in the same message, so the agent can act without a second lookup
- **UX-002**: Hook output MUST stay under one screen for a typical edit: one line per finding plus one fix paragraph per distinct rule
- **UX-003**: Path-mode output MUST show a per-rule summary table first and detail per file second, with disabled rules shown as `off` rather than `0`
- **UX-004**: Fix text MUST be the same wording the corresponding skill uses, so the agent sees one vocabulary

### Key Entities *(include if feature involves data)*

- **Rule**: an id (UNOLnnn), a short name used in reports and suppressions, a title, fix text, a minimum profile, and whether it requires Toolkit
- **Finding**: a rule, a file, a line, a message, and an effective severity after profile, `.editorconfig` and Toolkit gating are applied
- **Profile**: the default rule set; `recommended` or `strict`
- **Project context**: whether Uno.Toolkit and Uno.Extensions.Navigation are referenced, detected from project files and XAML namespaces
- **Info counters**: non-gating measurements reported alongside findings

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A hook invocation on a single edit completes in under 250 ms on a developer laptop (measured: about 130 ms)
- **SC-002**: A full scan of a 175-file app completes in under 2 seconds (measured: 0.48 s)
- **SC-003**: The recommended rules stay under 5% false positives when hits are reviewed by hand across at least eight apps, including the prototype author's samples
- **SC-004**: Zero edits are blocked or rolled back by the hook across all runs
- **SC-005**: On agent-built sample apps, the HEX count per app drops to zero within one agent turn after the hook is installed
- **SC-006**: The hook, the CLI and the MCP tool report identical counts on the same corpus
- **SC-007**: Every rule has at least one positive and one negative unit test and one regression test per false positive found in the field (currently 32 tests)

### Qualitative Outcomes

- **SC-008**: Agents fix findings without asking the developer what the fix means, because the message names the replacement
- **SC-009**: Teams on MVVM or without Toolkit keep the tool enabled after turning off the rules that do not fit, rather than removing it
- **SC-010**: The rule list is treated as owned and curated: a new rule needs a repro and a corpus measurement before it ships in `recommended`

## Evidence from the prototypes

Two implementations exist. The first is a PowerShell 7 script (368 lines, regex over raw text, wired as a Claude Code PostToolUse hook) in daily use on sample apps. The second is the C# port in [`tools/uno-lint`](../../tools/uno-lint/), built on 2026-09-30, which reproduces the script's counts exactly on every app except where it removes a false positive, and runs four times faster. Its [FINDINGS.md](../../tools/uno-lint/FINDINGS.md) has the full run.

### Corpus

| App | Files | Toolkit | Author |
|---|---|---|---|
| uno.toolkit.ui Uno.Toolkit.Samples | 175 | yes | Uno team, gallery app |
| Uno.Themes ThemesSampleApp | 14 | no | Uno team |
| BrewHouse (coffee shop sample) | 28 | yes, SimpleTheme + Toolkit + MVUX | agent |
| GridWatch (utility dashboard sample) | 28 | yes | agent |
| TestApp (counter template) | 6 | no | agent |

### Results, strict profile

| Rule | Toolkit samples | Themes | BrewHouse | GridWatch | TestApp |
|---|---|---|---|---|---|
| HEX | 17 | 0 | 93 | 0 | 0 |
| TOKENTHEME | 4 | 0 | 0 | 18 | 0 |
| ICON | 5 | 0 | 0 | 0 | 0 |
| BUILTIN | 0 | 0 | 0 | 0 | 0 |
| RESPONSIVE | 0 | 0 | 0 | 0 | 0 |
| CARD | 5 | off | 16 | 17 | off |
| BACKBAR | 0 | off | 0 | 0 | off |
| CODEBEHIND | 129 | 5 | 0 | 16 | 1 |
| OVERLAY | 0 | 0 | 0 | 0 | 0 |

BrewHouse had SimpleTheme, Toolkit and MVUX enabled and still hard-coded 93 colors across four pages. This is the failure the linter exists for. The 129 CODEBEHIND hits on Uno's own Toolkit gallery are why the architectural rules are strict-only.

### False positives removed by the C# port

| Rule | Prototype | Port | Cause |
|---|---|---|---|
| TOKENTHEME, Toolkit | 7 | 4 | a brush with `Color="{ThemeResource ...}"` follows the theme |
| CARD, Toolkit | 9 | 5 | `CornerRadius="8,0,0,0"` is a color swatch, not a card |
| RESPONSIVE, Toolkit | 1 | 0 | `ActualHeight > 0` asks whether layout ran |
| HEX + TOKENTHEME, GridWatch | 18 + 18 | 0 + 18 | one brush defined once with a hex literal is one problem |
| Suppression | reason-less `allow` accepted | rejected | `-->` matched as the reason |

### Performance, same machine

| Run | PowerShell | C# |
|---|---|---|
| Full scan, 175-file Toolkit samples | 2.8 s | 0.48 s |
| Hook, one edit | 460 to 490 ms | 127 to 135 ms |

### Per-rule verdict from reading the hits

- **HEX** (110 hits): no false positives found. Keep in recommended.
- **TOKENTHEME** (22 hits): survivors are brand and status brushes defined once; correct by the rule but often fixed by a rename. Keep in recommended, consider `suggestion` severity.
- **ICON** (5 hits): inline Material Design Icons path data. Correct.
- **CARD** (38 hits): still misfires on the Toolkit's documented ShadowContainer pattern and calls pills (`CornerRadius="20" Padding="12,6"`) cards when the fix is `utu:Chip`. Keep in strict until fixed.
- **CODEBEHIND** (151 hits): every hit is a real handler; whether that is a defect is architecture. Strict only.
- **BUILTIN, RESPONSIVE, BACKBAR, OVERLAY**: zero hits on this corpus; unit-tested only. Need more apps to calibrate.

## Architecture and Delivery

One C# rule library, and every surface is a thin host around it. Rules, profiles, severities and suppressions are written once and behave the same whether the caller is an agent hook, an MCP client or the compiler.

```
XAML files ──(XDocument)──┐                      ┌── uno-lint CLI + hook ── agents via plugin, CI   (phase 1)
C# files ───(Roslyn)──────┼── Uno.Lint.Core ─────┼── MCP tool on uno.devserver ── any MCP agent     (phase 2)
csproj, xmlns ─(gate)─────┘   netstandard2.0     └── Roslyn analyzer ── VS, Rider, dotnet build    (phase 3)
```

**Why C#, not PowerShell.** PowerShell cannot ship in the SDK, is Windows-first in practice, and regex misfires: comments and strings trigger C# rules, and context such as "is this Border inside a ControlTemplate" is approximated. `XDocument` makes ancestry exact; Roslyn makes only real syntax count; netstandard2.0 lets a Roslyn analyzer load the same assembly later.

**Surface 1, plugin hook and CLI (phase 1).** A `dotnet tool` named `uno-lint`. The `uno-platform-studio` plugin already installs into Claude Code, Copilot CLI and Codex CLI; adding a hooks manifest that calls `uno-lint --hook` ships the linter to all three in one plugin release. This is the Studio plugin route.

**Surface 2, MCP tool (phase 2).** `uno.devserver --mcp-app` already registers the Uno MCP server Studio users have. A `lint_project` / `lint_file` tool on it lets any agent call the linter on demand and lets skills say "run the linter before you report done".

**Surface 3, Roslyn analyzer (phase 3).** Uno.UI already ships `Uno000x` analyzers and the XAML generator emits `UXAML` diagnostics, so there is an established home. XAML rules need the analyzer to read `.xaml` as additional files, which the Uno XAML generator already does.

### Rollout

| Phase | Scope | Exit criteria |
|---|---|---|
| 1, now | Calibrate on more real apps; fix CARD pill and ShadowContainer misfires; ship hook via the Studio plugin; `dotnet tool` for CI | recommended rules under 5% false positives on 8+ apps; hook in a plugin release |
| 2, next | MCP tool on `uno.devserver`; three correctness rules from the runtime-gotchas list; skills call the linter before reporting done; track counts across sample apps | hook and MCP tool agree on every count; each correctness rule has a repro |
| 3, later | Roslyn analyzer NuGet; same rules, same `.editorconfig`; warnings in VS, Rider and `dotnet build`; opt-in from the Uno templates | rule list stable for one release cycle before starting |

## Assumptions

1. **Hook availability**: Claude Code, Copilot CLI and Codex CLI plugins can declare post-edit hooks that receive the edited file path and the added text. Verified for Claude Code; to confirm for the other two before phase 1 ships.
2. **Binary distribution**: `dnx uno-lint --hook` needs no install step but adds first-run latency; a global tool install is faster but needs a setup step. Decide after measuring `dnx` cold start.
3. **Theme vocabulary**: role brush keys (`PrimaryBrush`, `SurfaceBrush`, `OutlineBrush`) resolve the same under SimpleTheme and Material, so fix text can name them without checking which theme is in use.
4. **Fix text ownership**: fix wording mirrors the skills in this repository and changes with them.
5. **Corpus access**: more agent-built apps will be made available to calibrate the four rules with zero hits so far.

## Design Decisions

1. **Two tiers, data-driven.** The recommended/strict split follows flutter_lints and Roslyn conventions and is justified by the 129 CODEBEHIND hits on Uno's own sample app, not by taste.
2. **Gate on project references, not a global switch.** A rule whose fix is a Toolkit control is off when the project has no Toolkit. This answers "should checks only run when the app uses Toolkit" per rule rather than globally.
3. **Report only the edited lines in hook mode.** Full-file context is used for evaluation; only findings inside the added text are returned. This keeps hook output short and avoids blaming the agent for old code.
4. **Never block.** The hook always exits 0. CI gates through the CLI exit code instead.
5. **Correctness rules next, not more style rules.** `x:Bind` against an MVUX ViewModel, `{Binding State.Value}`, and Uno0001 members in framework handlers are bugs; catching bugs earns the trust that lets style rules ride along.
6. **`.editorconfig` over a custom config file.** It is what every .NET analyzer uses and carries over unchanged to the analyzer surface.

## Dependencies

- `uno-platform-studio` plugin manifests for the three agent hosts (this repository)
- `uno.devserver` MCP server for the phase 2 tool
- Microsoft.CodeAnalysis.CSharp 4.x for the C# rules
- Uno.UI analyzer infrastructure for phase 3

## Open Questions

- [ ] **Tiering**: is the recommended/strict split right, or should CARD move to recommended once the pill and ShadowContainer misfires are fixed?
- [ ] **First surface**: plugin hook first (reaches three hosts in one release) or MCP tool first (reaches every agent with the Uno MCP)? Recommendation: plugin hook first, MCP tool in the same quarter.
- [ ] **Results to the IDE**: yes via phase 3; confirm nothing extra is wanted before then.
- [ ] **Rule list ownership**: proposal is the team that owns the plugin skills owns the rules, with the runtime-gotchas list as the intake queue.
- [ ] **TOKENTHEME severity**: warning or suggestion by default?

## Out of Scope

- Auto-fixing findings. The linter reports; the agent or developer rewrites.
- Style and formatting rules (indentation, attribute order) that other tools already cover.
- Rules requiring a running app or a visual tree. Those belong to the App MCP.
- Linting C# Markup (code-first UI) in this version.
