# Feature Specification: Uno Linter

**Feature Branch**: `001-uno-linter`  
**Created**: 2026-10-01  
**Status**: Draft  
**Input**: User description: "A deterministic linter for Uno Platform XAML and C# that verifies what an AI agent actually wrote, complementing the skills that tell it what to write. Ships as one rule library exposed through the Studio plugin hook, an MCP tool on the Uno dev server, and later a Roslyn analyzer."

## Summary

Skills advise, but nothing verifies. The `uno-platform-studio` plugin ships skills that tell an agent how to use MVUX, navigation, the Toolkit and theming, and agents still drift: context fills up, a skill is not invoked, or the model reasons its way back to a shortcut. Every sample built with the plugin so far came back with hard-coded hex colors, cards drawn as rounded Borders and icons drawn from shapes, despite skills saying otherwise.

The Uno Linter closes the loop. It reads XAML with a real XML parser and C# with Roslyn, applies a small set of semantic rules, and reports each hit as file, line, rule id, what was found and what to use instead. It runs right after each edit, within the hook budget (SC-001), so the agent fixes the problem in the same turn, never blocks an edit, and only reports the lines the edit added.

A working C# prototype exists under [`tools/uno-lint`](../../tools/uno-lint/), with unit tests for every rule and runs across real apps. The decisions this spec asks for are the rule tiers, the first shipping surface and who owns the rule list; the review rounds answered most of them, see Open Questions.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Agent gets corrected in the same turn (Priority: P1)

A developer is building an Uno app with the Studio plugin in Claude Code or Codex. The agent edits `CartPage.xaml` and adds a `Border` with `Background="#FFF5EFE6"` and `CornerRadius="16"`. Immediately after the edit, the linter runs on the saved file and hands the agent two findings: a hex literal where a theme brush belongs, and a Border drawn as a card where `utu:CardContentControl` belongs. The agent fixes both before moving to the next file. The developer never sees the shortcut.

**Why this priority**: This is the whole reason the product exists. Catching the pattern at the first edit stops it from being copied to every later screen, which is how one workaround becomes the app's architecture.

**Independent Test**: Can be fully tested by installing the plugin hook, asking an agent to add a colored rounded Border to a page, and verifying the agent receives the two findings as context and rewrites the element. Delivers value without any other surface.

**Acceptance Scenarios**:

1. **Given** the plugin hook is installed and an agent edits a `.xaml` or `.cs` file, **When** the edit adds a pattern a rule covers, **Then** the agent receives the findings for that edit as additional context within the same tool turn
2. **Given** an edit adds a hex literal and a hand-drawn card on the same line, **When** the hook runs, **Then** both findings are reported with the line number, the rule id and the replacement to use
3. **Given** an edit touches a file that already contains 40 older findings, **When** the hook runs, **Then** only findings inside the lines the edit added are reported
4. **Given** the linter finds problems, **When** the hook returns, **Then** the edit is never rejected or rolled back and the hook exits successfully
5. **Given** a file fails to parse or the linter itself throws, **When** the hook runs, **Then** the agent sees nothing, the edit proceeds unchanged, and one line naming the failure goes to standard error for whoever debugs the hook
6. **Given** the hook command passes an option the installed tool does not know, **When** the hook runs, **Then** it ignores the option with a note on standard error, still lints, and exits successfully

---

### User Story 2 - Team turns off rules that do not fit its architecture (Priority: P1)

A team builds MVVM apps with event handlers in code-behind and no Uno.Toolkit. They run the linter and see hundreds of CODEBEHIND hits and suggestions to use Toolkit controls they do not reference. They set one line in `.editorconfig` to turn CODEBEHIND off, and the Toolkit-only rules were already off because the project has no Toolkit reference. The remaining findings are ones they agree with.

**Why this priority**: Without this, half the rules produce noise for anyone not on MVUX + Extensions navigation + Toolkit, and the tool gets disabled. The Toolkit sample app alone produces well over a hundred CODEBEHIND hits.

**Independent Test**: Can be fully tested by running the linter on an app without Toolkit in strict mode and verifying CARD and BACKBAR report as off, then adding `dotnet_diagnostic.UNOL103.severity = none` and verifying CODEBEHIND disappears.

**Acceptance Scenarios**:

1. **Given** a project with no Uno.Toolkit reference in `UnoFeatures`, `PackageReference` or XAML namespaces, **When** the linter runs in any profile, **Then** CARD, BACKBAR and Toolkit-specific BUILTIN suggestions are disabled and reported as off
2. **Given** a `.editorconfig` with `dotnet_diagnostic.UNOL103.severity = none`, **When** the linter runs, **Then** CODEBEHIND findings are not reported
3. **Given** a `.editorconfig` with `uno_lint.profile = strict`, **When** the linter runs without a `--profile` switch, **Then** every rule is evaluated
4. **Given** no configuration, **When** the linter runs from the command line, **Then** the recommended profile applies and only the recommended rules (FR-012) report
5. **Given** a deliberate exception with a reason comment within three lines above the hit, **When** the linter runs, **Then** that hit is suppressed; **Given** the same comment without a reason, **Then** the hit is still reported

---

### User Story 3 - Maintainer audits an existing app and tracks counts (Priority: P2)

A sample-gallery maintainer runs the linter on a whole project before publishing. They get a count per rule and the top files for each, fix or suppress each hit, and the run exits clean. Over time they compare counts across apps to see which skills or templates still produce shortcuts.

**Why this priority**: Counts make the problem measurable. They tell us whether a new skill version reduced hard-coded colors, and they give CI a gate.

**Independent Test**: Can be fully tested by running `uno-lint lint <path>` on an app, verifying the per-rule table and exit code 1, fixing the hits, and verifying exit code 0.

**Acceptance Scenarios**:

1. **Given** a project folder, **When** the linter runs in path mode, **Then** it prints a count per rule, each rule's fix once, the top files per rule with sample findings (and how many more each file has), and the four info counters
2. **Given** findings at warning severity exist, **When** the run ends, **Then** the exit code is 1; **Given** none exist, **Then** it is 0; **Given** the path does not exist or an option is unknown or malformed, **Then** it is 2
3. **Given** the `--json` switch, **When** the run ends, **Then** output is the report (FR-020): root, profile, project references, file counts, whether it gates, a summary per rule, info counters, and one entry per finding and per suppressed finding
4. **Given** files under `bin`, `obj`, `.git`, or generated `*.g.cs`, **When** the linter runs, **Then** they are skipped; **Given** a folder named Dev, Harness, Probe(s) or Test(s), or ending in one (`MyApp.Tests`), **Then** its files are skipped unless `--include-dev` is passed

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

- A XAML file that is not well-formed (an unclosed tag, a close tag with the wrong casing, an unquoted attribute, a stray ampersand, truncated output) is repaired by Uno.XamlParser and linted, and counted as repaired; if the reader still stops, the elements read before the stop are linted and the file is counted as partial. Only a file the parser cannot read at all is counted as unparsed.
- An edit whose text cannot be located in the saved file (concurrent change, different path), or appears more than once, produces no findings rather than blaming the agent for lines it did not write.
- An agent copies an existing block that carries a suppression comment to a new place; the new copy is reported, because the added lines come from the patch, not from searching for the text.
- Line endings differ between the edit payload and the saved file (CRLF vs LF); the edit is still located.
- A brush whose `Color` is a `{ThemeResource}` follows the theme and is not a TOKENTHEME finding even though it is defined once.
- A `Border` with a radius on only some corners (`8,0,0,0`) is a swatch or joined edge, not a card.
- `ActualHeight > 0` asks whether layout has run; it is not a breakpoint.
- A brush defined once with a literal color is one problem; it is reported once, under HEX with a single-theme note, whether `Color=` sits on the same line as `x:Key` or the next one.
- `App.xaml.cs` with `OnSuspending(object, SuspendingEventArgs)` is not a CODEBEHIND finding; `MainPage.xaml.cs` with the same handler is.
- `App/.editorconfig` turning HEX off applies under `App/` whether the run is `uno-lint .` or `uno-lint App/MainPage.xaml`.
- `<UnoFeatures>Toolkit</UnoFeatures>` in a root `Directory.Build.props` enables CARD whether the run starts at the root, at `src/App`, or at a single file.
- An `allow hex` comment above a brush in `Page.Resources` does not reach a `Border` a few lines below `</Page.Resources>`.
- `uno-lint notes.txt` exits 2 instead of parsing the text as C#.
- A suppression comment with no reason (`allow hex -->`) does not suppress; the comment terminator is not a reason.
- A suppression comment above a multi-line element covers a hit on that element's fifth attribute line: XML has no place for a comment inside a start tag, so the element's start line is an anchor too.
- A project named `TestApp`, a `Content/TestPages/` folder or a page named `ProbeResultsPage.xaml` must not be skipped by the dev-path rule, which matches whole folder names only (`Dev`, `Harness`, `Probe(s)`, `Test(s)`, or a folder ending in `.Tests` and the like).
- `Text="#404"` is text, not a color; HEX only fires on color properties. `Foreground="Red"` is a color; `Transparent` is not reported.
- `CornerRadius="0"` is a plain box, a 48x48 Border with radius 24 is an avatar, a 32-high Border with radius 16 is a pill, and a radius of 12 or more around a single TextBlock is a chip. None is a card.
- `<StaticResource x:Key="AccentAliasBrush" ResourceKey="PrimaryBrush"/>` follows whatever its target does and is not a TOKENTHEME finding.
- An `.editorconfig` section `[App/Tests/**]` turning HEX off applies to files under that folder only, exactly as a Roslyn analyzer would read it.
- `{Binding Pair.Value}` inside a `DataTemplate` is left alone: the item may really have a `Value` property. Outside templates, in an MVUX project, `X.Value` is the state trap.
- A file whose lines end in a lone CR (or a mix of CR, LF and CRLF) puts findings and allow comments on the same lines XmlReader and Roslyn use.
- A file outside any project (no `.csproj`, `.sln` or `.slnx` above it) is linted with no project references and only the `.editorconfig` files above it; its folder is not walked.
- A folder containing a symbolic link or junction back up the tree is scanned once; links are not followed.
- A XAML file with a document type declaration is not read: XAML has none, and no entity is ever expanded into a message.
- A message never echoes an `x:Name` or `x:Key` that is not shaped like an identifier.
- A malformed XAML file whose repair changes its line count is reported as such in the hook, without attributing its findings to the edit.
- A Codex patch that adds one file and updates another is linted file by file; a path or edit with non-ASCII characters is read as UTF-8 whatever the console code page.

## Requirements *(mandatory)*

### Functional Requirements

#### Rules

- **FR-001**: System MUST detect hard-coded colors on color properties (property or element named for a color, brush, background, foreground, fill, stroke or tint, including `Setter` and keyframe values targeting one): hex literals and the WinUI named colors except `Transparent`, outside palette files and theme override files; a file is a palette file when a whole word of its name is `Palette`, `Colors` or `Tokens` (`AppColors.xaml`, `BrandPalette.Dark.xaml`, not `ColorPickerPage.xaml` or `AuthTokenPage.xaml`), and a theme override file when its name contains `ColorOverride` or `ColorPaletteOverride`; keyed resources inside `ResourceDictionary.ThemeDictionaries` are exempt. In C#, `Colors.<Name>`, `Color.FromArgb(...)` and `ColorHelper.FromArgb(...)` are reported under the same rule, with the same palette-file exemption by file name (`Palette.cs`, `AppColors.cs`). Inside `ThemeDictionaries`, anything nested under a keyed resource (a `GradientStop` in a keyed brush, a `Setter` in a keyed `Style`, a property element) is exempt, not only the keyed element itself (HEX, UNOL001)
- **FR-002**: System MUST detect app-defined `*Brush` resources outside `ThemeDictionaries` whose color is fixed, excluding keys named `*Invariant`, `*OnDark`, `*OnLight` or `*Fixed`, brushes whose color is a `{ThemeResource}`, and `StaticResource`/`ThemeResource` alias elements; default severity is `suggestion`. When such a brush carries a literal color and HEX is active on the file, the hit is reported once under HEX (the gating rule) with a note that the brush is also single-themed, keyed on the element so formatting does not change gating; `allow hex` suppresses it (TOKENTHEME, UNOL002)
- **FR-003**: System MUST detect inline path-mini-language `Data` on `Path`/`PathIcon`, inline `PathGeometry`/`GeometryGroup`, and basic shapes composed inside a `Viewbox`, outside icon files (a whole word of the name is `Icons`: `Icons.xaml`, `MaterialIcons.xaml`, not `LexiconPage.xaml`) and outside `ControlTemplate` elements (ICON, UNOL003)
- **FR-004**: System MUST detect classes deriving from a control base type whose name contains a platform or Toolkit control as a whole PascalCase word (`StarRating`, `RecipeCard`; not `OperatingHoursView` or `DiscardBanner`) and whose companion XAML does not already use that control (BUILTIN, UNOL004)
- **FR-005**: System MUST detect relational comparisons against `ActualWidth`, `ActualHeight`, `NewSize` or `Bounds` in `*.xaml.cs`, excluding comparisons against zero (RESPONSIVE, UNOL005)
- **FR-006**: System MUST detect `Border` elements with a uniform non-zero `CornerRadius` and a `Background`, `BorderBrush` or `Padding`, outside `ControlTemplate` elements, treating as not a card: a Border without Background inside `ShadowContainer`, a circle (explicit equal Width and Height with radius at least half of them), a pill (radius at least half an explicit Height), and a chip (radius 12 or more around no child or a single TextBlock, FontIcon, SymbolIcon, PathIcon or Run) (CARD, UNOL101)
- **FR-007**: System MUST detect `Navigation.Request="-"` in a XAML file that contains no `NavigationBar` (BACKBAR, UNOL102)
- **FR-008**: System MUST detect event-handler methods (exactly two parameters: `object` first, a type whose name ends in `EventArgs` or `Args` second) and `.Visibility =` assignments in page and control `*.xaml.cs`; `App.xaml.cs` and classes deriving from `Application` are exempt, since they host the lifecycle handlers the template ships (CODEBEHIND, UNOL103)
- **FR-009**: System MUST detect `.Visibility =` assignments whose target name ends in Overlay, Sheet, Scrim, Drawer or Popup and report them as OVERLAY (UNOL104) instead of CODEBEHIND; `Panel` is a plain layout suffix and stays CODEBEHIND
- **FR-010**: System MUST report four non-gating info counters: references to app-defined brush keys, AdaptiveTrigger count against `utu:Responsive` count, app-authored ControlTemplate count, and `WORKAROUND(` marker count
- **FR-011**: Each finding MUST carry the rule id, rule name, file path relative to the lint root, 1-based line, a message naming what was found, and the fix text naming what to use instead, in every output (text, JSON, hook context)
- **FR-023**: In projects that reference MVUX (`UnoFeatures` `Mvux`, `Uno.Extensions.Reactive`, or its xmlns), System MUST detect `{Binding X.Value}` and `{Binding Path=X.Value}` outside `DataTemplate` elements and name the state to bind instead (BINDVALUE, UNOL201, recommended)
- **FR-024**: System MUST count suppressed hits per rule and report them next to the active counts in text and JSON, so suppression is a visible metric rather than a silent escape hatch; suppressed hits of disabled rules are not counted
- **FR-025**: Fix text MUST adapt to the project: HEX names Uno.Themes role brushes only when Uno.Themes (Material, Cupertino or SimpleTheme) is referenced and names WinUI theme brushes otherwise; Toolkit controls are named only when the Toolkit is referenced

#### Profiles and configuration

- **FR-012**: System MUST provide a `recommended` profile enabling HEX, TOKENTHEME, ICON, BUILTIN, RESPONSIVE and BINDVALUE, and a `strict` profile enabling all ten rules
- **FR-013**: System MUST default to `recommended` in path mode and to `strict` in hook mode unless a profile is given explicitly
- **FR-014**: System MUST read `dotnet_diagnostic.UNOLnnn.severity = default|none|silent|suggestion|warning|error` from every `.editorconfig` that governs a file: for a file, the chain above it (stopping at `root = true`); for a folder, the chain above it and the files below it, so a nested `App/.editorconfig` applies whether the run starts at the repository root or at the file and the CLI and the hook agree. `default` restores the rule's default severity for the active profile, as it does for the compiler. Section globs are honored per file with EditorConfig semantics (`[*.xaml]`, `[App/Tests/**]`, `[*.{xaml,cs}]`), nearest file and last matching section winning. A severity key above any section header is ignored and reported, since a Roslyn analyzer does not read it there. `uno_lint.profile = recommended|strict` is project-wide
- **FR-015**: System MUST disable rules whose fix is a Toolkit control when the project has no Uno.Toolkit reference, detected from `UnoFeatures` or `PackageReference` in the project's csproj files and in every `Directory.Build.props` / `Directory.Packages.props` above the project root (where the Uno template puts `UnoFeatures`), or from the `using:Uno.Toolkit.UI` xmlns, with `--toolkit` and `--no-toolkit` overrides; the same sources feed the MVUX and Uno.Themes gates
- **FR-016**: System MUST suppress a single finding when a comment on the same line, or in the comment lines directly above it (up to three, stopping at the first line that is not a comment so a comment never reaches past another element or statement), reads `uno-lint: allow <rule> - <reason>`; for a finding on an attribute of a multi-line element the element's start line counts as the anchor too; `allow all - <reason>` covers every rule at that anchor; the reason is mandatory; the legacy `xaml-lint:` prefix MUST be accepted

#### Surfaces

- **FR-017**: System MUST provide a command-line tool with three commands built on one command framework, so help, option binding, structured output and exit codes come from one definition: `lint [path]` (a folder or a `.xaml`/`.cs` file, with `--profile`, `--top`, `--include-dev`, `--toolkit`/`--no-toolkit`, `--no-cache`, and `--json` or another structured format), `rules` (the catalog: id, name, category, lowest profile, project gate, default severity) and `hook`. `lint` exits 0 when clean, 1 when findings at warning or above remain, 2 when the run cannot complete (unknown or malformed option, missing path, a file of any other type). A single file is linted with the same project root, dev-path skip and `.editorconfig` chain a directory run would give it, so the CLI and the hook agree on every file
- **FR-018**: System MUST provide a `hook` command that reads an agent harness's PostToolUse payload on standard input as UTF-8, lints each edited file for full context, keeps only findings on the lines the edit added, and replies with `systemMessage` and `hookSpecificOutput.additionalContext`, leading with bugs (correctness rules) before patterns the platform covers, naming each rule's fix, without offering suppression, and noting any allow comments the edit itself added. One adapter per harness turns the payload into the edited files and their added lines: Claude Code (Edit, MultiEdit, Write) and Codex (`apply_patch`, possibly several files, paths relative to the payload's `cwd`). The hook always exits 0: an unknown option is ignored with a note on standard error, and any failure is one line on standard error and nothing on standard output. A file whose XAML repair shifted its lines is named as malformed instead of having findings attributed to the edit
- **FR-019**: The Claude Code adapter MUST take the added lines from `tool_response.structuredPatch` when present (every line for a Write that created the file). Without one, it MUST diff `old_string` against `new_string` so unchanged context lines are not reported, locate `new_string` in the saved file regardless of CRLF/LF differences and re-indentation, and report nothing when the text is not found or is found more than once without `replace_all`. The Codex adapter MUST treat every line of an added file as added, locate each update hunk's new text in the saved file the same way, and skip deleted files
- **FR-020**: Every surface MUST render one immutable report built once per run (the CLI's text and JSON output now, the MCP tool on the Uno dev server in phase 2), so the shapes cannot drift; a test pins the JSON shape
- **FR-021**: System MUST expose the same rules as a Roslyn analyzer package honoring the same `.editorconfig` keys (phase 3)
- **FR-022**: The rule functions MUST be host-agnostic and never read the disk, parse files or load configuration themselves. Three layers: a rules-only library (the C# rules on a Roslyn `SyntaxTree`, the shared predicates, suppression parsing, the catalog and the model) targeting netstandard2.0 and the lowest Roslyn the analyzer must load in (currently 4.8); the XAML rules as Uno.XamlParser Stage-1 fixers that only report (net9.0, like the parser); and a net10.0 host that walks folders, reads `.editorconfig`, detects the project context, builds the report and adapts hook payloads. Another Uno.XamlParser host (Studio Live, Uno.AI) runs the XAML rules with three public steps: register them on its pipeline, run Stage-0 repairs only when the text is not well-formed, and pass the result through the same post-processing uno-lint uses (rule gating, the HEX/TOKENTHEME hand-off, suppression comments, line order). A pipeline processes one document at a time; a host that shares one across threads serializes its calls, as uno-lint does, and every call can be cancelled

### UX Requirements *(mandatory for UI features)*

This feature has no graphical UI. Its user-facing surface is text consumed by agents and developers.

- **UX-001**: Every finding returned to an agent MUST name the replacement in the same message, so the agent can act without a second lookup
- **UX-002**: Hook output MUST stay under one screen for a typical edit: one line per finding plus one fix paragraph per distinct rule
- **UX-003**: Path-mode output MUST show a per-rule summary table first and detail per file second, with disabled rules shown as `off` rather than `0`
- **UX-004**: Fix text MUST be the same wording the corresponding skill uses, so the agent sees one vocabulary

### Key Entities *(include if feature involves data)*

- **Rule**: an id (UNOLnnn), a short name used in reports and suppressions, a title, fix text, a category (a bug, or a pattern the platform covers), a minimum profile, and whether it requires Toolkit or MVUX
- **Finding**: a rule, a file, a line, a message, and an effective severity after profile, `.editorconfig` and project gating are applied; immutable
- **Report**: one run's outcome as one immutable value that every output renders
- **Hook request**: the files an agent edit wrote and the lines it added, produced by one adapter per harness
- **Profile**: the default rule set; `recommended` or `strict`
- **Project context**: whether Uno.Toolkit, Uno.Extensions.Navigation, MVUX (Uno.Extensions.Reactive) and Uno.Themes (Material, Cupertino, SimpleTheme) are referenced, detected from the csproj files, the `Directory.Build.props` / `Directory.Packages.props` chain above the project, and XAML namespaces
- **Info counters**: non-gating measurements reported alongside findings

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A hook invocation on a single edit completes in under 250 ms on a developer machine (see [Performance](#performance))
- **SC-002**: A full scan of a 200-file app completes in under 2 seconds (see [Performance](#performance))
- **SC-003**: The recommended rules stay under 5% false positives when hits are reviewed by hand across at least eight apps, including the prototype author's samples
- **SC-004**: Zero edits are blocked or rolled back by the hook across all runs
- **SC-005**: On agent-built sample apps, the HEX count per app drops to zero within one agent turn after the hook is installed, with suppressions under 10% of the original hits; a zero reached by allow comments does not count
- **SC-006**: The hook, the CLI and the MCP tool report identical counts on the same corpus, including under sectioned `.editorconfig` files
- **SC-007**: Every rule has at least one positive and one negative unit test and one regression test per false positive found in the field

### Qualitative Outcomes

- **SC-008**: Agents fix findings without asking the developer what the fix means, because the message names the replacement
- **SC-009**: Teams on MVVM or without Toolkit keep the tool enabled after turning off the rules that do not fit, rather than removing it
- **SC-010**: The rule list is treated as owned and curated: a new rule needs a repro and a corpus measurement before it ships in `recommended`

## Evidence from the prototypes

Two implementations exist. The first was a PowerShell script, a regex pass over raw text wired as a Claude Code PostToolUse hook; it is not in this repository. The second is the C# tool in [`tools/uno-lint`](../../tools/uno-lint/). On the apps both were run on, the C# tool reproduced the script's counts except where it removed a false positive (listed below). [FINDINGS.md](../../tools/uno-lint/FINDINGS.md) has the corpus results and the per-rule verdicts.

### Corpus

Two public apps anyone can re-run, and three agent-built Studio sample apps that are not public.

| App | Revision | Files | References |
|---|---|---|---|
| [uno.toolkit.ui](https://github.com/unoplatform/uno.toolkit.ui) `samples/` | `c4c8f887` | 203 | Toolkit, Uno.Themes |
| [Uno.Themes](https://github.com/unoplatform/Uno.Themes) `src/samples/` | `27fb1a61` | 192 | Uno.Themes |
| Three agent-built samples (a coffee shop, a utility dashboard, the counter template) | internal | 6 to 28 each | two with Toolkit and MVUX |

### Results, strict profile, public apps

`uno-lint lint <folder> --profile strict`; "off" means a project gate turns the rule off.

| Rule | Toolkit samples | Uno.Themes samples |
|---|---|---|
| HEX | 287 | 23 |
| TOKENTHEME | 4 | 4 |
| ICON | 5 | 46 |
| BUILTIN | 0 | 0 |
| RESPONSIVE | 0 | 0 |
| CARD | 4 | off |
| BACKBAR | 0 | off |
| CODEBEHIND | 145 | 30 |
| OVERLAY | 0 | 0 |
| BINDVALUE | off | off |

Most Toolkit HEX hits are named colors used as demo fills in gallery pages, correct by the rule; a gallery would turn HEX off for its samples folder in `.editorconfig`. The CODEBEHIND count on Uno's own gallery is why the architectural rules are strict-only.

On the agent-built samples, an app with SimpleTheme, Toolkit and MVUX enabled still hard-coded its whole palette on every page: HEX was by far its largest count. This is the failure the linter exists for.

### False positives removed by the C# tool

| Rule | Cause |
|---|---|
| TOKENTHEME | a brush with `Color="{ThemeResource ...}"` follows the theme |
| CARD | `CornerRadius="8,0,0,0"` is a color swatch, not a card |
| RESPONSIVE | `ActualHeight > 0` asks whether layout ran |
| HEX + TOKENTHEME | one brush defined once with a literal color is one problem, reported once |
| HEX | `<SolidColorBrush x:Key="Red">` names a resource; the key is not a color |
| Suppression | a reason-less `allow` was accepted because `-->` matched as the reason |

### Performance

Measured once, on 2026-10-19: Windows 11 on an Intel Xeon E-2288G (8 cores, 3.7 GHz), the Release build on the .NET 10 runtime with JIT (no ReadyToRun). Each figure is the median of repeated runs after one warm-up run, timed from process start to exit.

| Run | Median |
|---|---|
| Hook, one Write of a 340-line page with 45 findings, project context cached (15 runs) | 158 ms |
| Full scan, Toolkit samples above, 203 files, strict (7 runs) | 888 ms |

The build before the command-framework and host rework took 468 ms and 879 ms on the same machine and method. Every other latency figure in the repository points here.

### Per-rule verdict from reading the hits

- **HEX**: no false positives found. Keep in recommended.
- **TOKENTHEME**: survivors are brand and status brushes defined once; correct by the rule but often fixed by a rename. Recommended, at `suggestion` severity.
- **ICON**: inline icon path data. Correct.
- **CARD**: calls the Toolkit's documented ShadowContainer pattern a card (open question). Strict only.
- **CODEBEHIND**: every hit is a real handler; whether that is a defect is architecture. Strict only.
- **BINDVALUE**: no hits; the MVUX samples bind states correctly. Unit-tested only.
- **BUILTIN, RESPONSIVE, BACKBAR, OVERLAY**: no hits on this corpus; unit-tested only. Need more apps to calibrate.

## Architecture and Delivery

XAML is read and its rules run inside Uno.XamlParser, the pipeline Studio Live and Uno.AI already run after every agent XAML write. uno-lint keeps what the parser does not do: the C# rules on Roslyn, and the host that applies `.editorconfig` severities, profiles, project gates and suppressions to both and builds one report. Rules, profiles, severities and suppressions behave the same whether the caller is the agent hook, an MCP client or, for C#, the compiler.

```
                         Uno.Lint.Xaml (net9.0)
XAML ─► Uno.XamlParser ─► XAML rules (Stage-1 fixers) ─► post-processing ─┐
                                                                           │  Uno.Lint.Host (net10.0)            Uno.Lint.Cli
C# ──► Roslyn ─────────► C# rules ─────────────────────────────────────────┼─► Linter ─► LintReport ─┬─► text, JSON  (lint, rules)
                         Uno.Lint.Core (netstandard2.0):                   │  .editorconfig,          └─► hook reply  (hook)
                         rules, catalog, suppression, model                │  project context              ▲
csproj, Directory.Build.props, .editorconfig ──────────────────────────────┘  hook adapters ───────────────┘  Claude Code, Codex
```

**Why C#, not PowerShell.** PowerShell cannot ship in the SDK, is Windows-first in practice, and regex misfires: comments and strings trigger C# rules, and context such as "is this Border inside a ControlTemplate" is approximated. Uno.XamlParser makes XAML ancestry exact and repairs malformed markup; Roslyn makes only real syntax count.

**XAML on Uno.XamlParser.** The six XAML rules are `IXamlSemanticFixer` implementations that only report, registered through the parser's `SemanticFixerTypes` list, plus one stream visitor for element text. `UnoLintXaml` gives any XamlParser host the three steps of FR-022: `AddTo(options)` registers the rules, `IsWellFormed(text)` decides whether to run the Stage-0 sanitizers (repairs are for malformed text, so a valid file is never rewritten; before 0.10.0-dev.80 a sanitizer also corrupted valid markup with `:/` in element text), and `Collect(result, ...)` produces the findings uno-lint itself reports. Against the earlier XDocument reader: identical findings on the corpus except six false positives removed, and malformed agent markup linted instead of skipped. [`tools/uno-lint/XAMLPARSER.md`](../../tools/uno-lint/XAMLPARSER.md) describes how the rules use the package.

**What is shared and what is not.** The C# rules take a Roslyn `SyntaxTree` plus a `ProjectContext` and never read the disk; `CSharpFile` accepts a compiler-provided tree, so a Roslyn analyzer can host them later (RS1035 forbids file IO in analyzers). The rules-only library holds them with the catalog, the shared predicates (`RuleLogic`), suppression parsing and the immutable model, so an analyzer package ships no file-system code. The XAML rules take XamlParser nodes. The folder walk, `.editorconfig` reader, project-context detection and its cache, the report and the hook adapters belong to the net10.0 host, implemented the way the compiler reads `.editorconfig` so a future analyzer agrees.

**Command surface.** The CLI is a thin layer on a command framework ([Repl Toolkit](https://github.com/yllibed/repl), dependency-free core): one command graph gives typed option binding with typo suggestions, generated help, structured output (`--json`, `--output:yaml`...), the 0/1/2 exit codes, and an in-memory test harness; the same graph can be exposed as MCP tools later. Handlers return the host's immutable `LintReport`; the hook command reads standard input as raw UTF-8.

**Surface 1, plugin hook and CLI (phase 1).** A `dotnet tool` named `uno-lint`. The `uno-platform-studio` plugin already installs into Claude Code and Codex CLI; a hooks manifest that runs `uno-lint hook` through a small shell shim (silent when the tool is not installed, with a `UNO_LINT_CLI` override) ships the linter to both in one plugin release. Copilot CLI and Gemini CLI post-edit hooks use other tool names (`edit`, `create`), a top-level `additionalContext`, and, for Gemini, a timeout in milliseconds; each needs its own adapter and snippet before it is supported. Inside Studio Live itself, the XAML rules can run in its existing XamlParser pass instead.

**Surface 2, MCP tool (phase 2).** `uno.devserver --mcp-app` already registers the Uno MCP server Studio users have. A `lint_project` / `lint_file` tool on it returns the same `LintReport`, lets any agent call the linter on demand and lets skills say "run the linter before you report done".

**Surface 3, Roslyn analyzer (phase 3).** For the C# rules. Uno.UI already ships `Uno000x` analyzers, so there is an established home. The XAML rules leave this goal: Uno.XamlParser is net9.0 and cannot load in an analyzer, and the Uno XAML generator's `UXAML` diagnostics already cover XAML in build and IDE.

### Rollout

| Phase | Scope | Exit criteria |
|---|---|---|
| 1, now | Calibrate on more real apps; CARD chip/avatar/zero-radius heuristics (done), ShadowContainer decision; BINDVALUE correctness rule (done); suppression counts (done); Claude Code and Codex hook adapters (done); publish `Uno.Lint.Tool` to nuget.org and ship the hook through the Studio plugin with the shim; `dotnet tool` for CI | recommended rules under 5% false positives on 8+ apps; suppressions under 10% of hits on agent-built apps; hook in a plugin release |
| 2, next | MCP tool on `uno.devserver`; two more correctness rules (`x:Bind` against an MVUX ViewModel, Uno0001 members in framework handlers); skills call the linter before reporting done; track counts and suppression rates across sample apps | hook and MCP tool agree on every count; each correctness rule has a repro |
| 3, later | Roslyn analyzer NuGet; same rules, same `.editorconfig`; warnings in VS, Rider and `dotnet build`; opt-in from the Uno templates | rule list stable for one release cycle before starting |

## Assumptions

1. **Hook availability**: Claude Code and Codex plugins can declare post-edit hooks that receive the edited files and enough of the edit to find the added lines. Verified end to end for Claude Code; the Codex adapter is built and tested from the payload the Codex hooks documentation describes (`apply_patch` in `tool_input.command`, `cwd` at the top level) and needs one live run before phase 1 ships. Copilot CLI and Gemini CLI are out of phase 1 (see Surface 1).
2. **Binary distribution**: the plugin installs the global tool (`dotnet tool install -g Uno.Lint.Tool`) and its hook calls `uno-lint hook` through the shim. `Uno.Lint.Tool` is not on nuget.org yet: CI packs it but does not publish it, so publishing is a phase 1 task. A zero-install `dotnet dnx` variant is not offered: without a pinned version and a feed the user owns, it resolves the package through the `nuget.config` of whatever project the agent is editing, so a cloned repository could substitute its own build and have it run on the first edit; it is also several times slower per call.
3. **Theme vocabulary**: role brush keys (`PrimaryBrush`, `SurfaceBrush`, `OutlineBrush`) resolve the same under SimpleTheme, Material and Cupertino, but do not exist in a Fluent-only app without Uno.Themes. Fix text is therefore gated on an Uno.Themes reference (FR-025); a Fluent-only app is pointed at WinUI theme brushes instead.
4. **Fix text ownership**: fix wording mirrors the skills in this repository and changes with them.
5. **Corpus access**: more agent-built apps will be made available to calibrate the four rules with zero hits so far.

## Design Decisions

1. **Two tiers, data-driven.** The recommended/strict split follows flutter_lints and Roslyn conventions and is justified by the CODEBEHIND count on Uno's own Toolkit gallery, not by taste.
2. **Gate on project references, not a global switch.** A rule whose fix is a Toolkit control is off when the project has no Toolkit. This answers "should checks only run when the app uses Toolkit" per rule rather than globally.
3. **Report only the edited lines in hook mode.** Full-file context is used for evaluation; only findings inside the added text are returned. This keeps hook output short and avoids blaming the agent for old code.
4. **Never block.** The hook always exits 0. CI gates through the CLI exit code instead.
5. **One correctness rule ships in phase 1.** `{Binding State.Value}` (BINDVALUE) is in the prototype now; `x:Bind` against an MVUX ViewModel and Uno0001 members in framework handlers follow in phase 2. Launching with only style rules invited pushback; catching a bug on day one earns the trust that lets the style rules ride along. The hook lists bugs before patterns so the agent fixes the bug first.
6. **`.editorconfig` over a custom config file.** It is what every .NET analyzer uses and carries over unchanged to the analyzer surface, including section globs.
7. **Suppression is a metric, and the agent is not offered it.** In early use, a hook that mentioned allow comments saw agents answer a large share of hits with one instead of a fix. The hook leads with the fix and says nothing about suppression; suppressions the agent writes anyway are counted per rule in every report.

## Dependencies

- `uno-platform-studio` plugin manifests for Claude Code and Codex (this repository)
- `uno.devserver` MCP server for the phase 2 tool
- Microsoft.CodeAnalysis.CSharp 4.x for the C# rules
- Uno.XamlParser for the XAML rules; Repl Toolkit for the command surface
- Uno.UI analyzer infrastructure for phase 3

## Open Questions

- [x] **Tiering**: CARD stays strict until the chip, avatar and zero-radius cases are proven on more apps. The heuristics are in; the measurement is pending.
- [x] **First surface**: plugin hook first, with the installed tool (Assumption 2).
- [ ] **Results to the IDE**: yes via phase 3; confirm nothing extra is wanted before then.
- [x] **Rule list ownership**: skill owners own the fix text (UX-004); the rule list sits with the plugin team, the runtime-gotchas list is the intake queue.
- [x] **TOKENTHEME severity**: `suggestion` by default; implemented.
- [ ] **Where the code lives**: keep `tools/uno-lint` in this repository with its own CI job, or move it to its own repository once the plugin hook ships? The spec stays here either way.
- [ ] **ShadowContainer**: a Border with Background and CornerRadius inside `utu:ShadowContainer` is the Toolkit's documented pattern and still reports CARD. Exempt it, or change the message to point at `ElevatedCardContentControlStyle`?
- [x] **Uno.XamlParser as the XAML host**: all XAML rules run inside Uno.XamlParser as Stage-1 fixers; the XDocument reader is removed; C# rules and the host stay in uno-lint.
- [ ] **Uno.XamlParser releases**: the tool depends on a prerelease package. Parser issues found while hosting the rules are filed with its maintainers; until a release carries the fixes, uno-lint keeps its workarounds (the well-formedness gate, the element-text visitor). Who owns releases while the hook depends on the package?

## Out of Scope

- Auto-fixing findings. The linter reports; the agent or developer rewrites.
- Style and formatting rules (indentation, attribute order) that other tools already cover.
- Rules requiring a running app or a visual tree. Those belong to the App MCP.
- Linting C# Markup (code-first UI) in this version.
