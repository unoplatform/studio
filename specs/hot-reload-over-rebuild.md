# Spec: Uno agent uses Hot Reload instead of rebuilding

2026-10-21

## Problem

The `uno-dev` agent often runs `dotnet build` or restarts the app after a `.xaml` or `.cs` edit, even though Hot Reload has already applied it. Each unneeded rebuild costs a build, a relaunch and the app state under test.

**Goal:** while an app runs under the App MCP, the agent confirms an edit with `uno_app_hot_reload_status` and rebuilds or restarts only in the cases the DevServer reports (rude edit, internal error) or the existing restart list allows (launch arguments, platform, `.csproj` or package change, startup code, Release build).

**Success:** in the edit-then-verify eval scenarios, no `dotnet build`, `uno_app_close` or `uno_app_start` follows an edit that Hot Reload applied, on Claude Code and on Codex. A close and relaunch that a persistence check calls for (`verification.md:33`) is not a failure.

## Background: Flutter's Agent Rules

Flutter's hot reload rule is advice, scoped to Dart files, and its Claude Code plugin does not ship it. Copying it as-is would not fix our problem.

| Aspect | Flutter ([flutter-hot-reload.md](https://github.com/flutter/agent-plugins/blob/main/rules/flutter-hot-reload.md)) | Uno today |
| --- | --- | --- |
| When it loads | Only for `**/lib/**/*.dart` (`trigger: glob`; Cursor copy has `alwaysApply: false`) | `uno-testing` skill when loaded; PostToolUse hook after each edit |
| What it asks | Call the `hot_reload` or `hot_restart` MCP tool after an edit | Do not build; confirm with `uno_app_hot_reload_status` |
| Enforcement | None: plain text; only Stop hooks for `dart format` and `dart analyze` | Hook reads live DevServer status and blocks on a compile failure |
| Claude Code | Not delivered: plugins cannot ship rules ([plugin reference](https://code.claude.com/docs/en/plugins-reference)) | Hook runs; skills load on demand |
| Codex | Not delivered: manifest has no rules | No hooks; skill text only |

The models differ. Flutter reloads only when the agent calls a tool, so it needs a "do this after an edit" rule. Uno's DevServer applies an edit on save, so we need a "do not rebuild" rule. That rule must be in front of the agent when it plans its steps, before the edit.

## Root cause

The skill the agent treats as its rulebook tells it to build, and the Hot Reload guidance sits where the agent sees it late or not at all. `uno-dev.agent.md` says `uno-build-app` "owns the process and the rules", so the agent follows it.

Where `uno-build-app` says build:

- `skills/uno-build-app/SKILL.md` rule 4: "Build and exercise the first feature before repeating its pattern."
- `SKILL.md` step 6: "Build and exercise."
- `SKILL.md:42`, the Trivial edit row: "Make the edit and build."
- `references/verification.md:14-21`: "Build and exercise every task-relevant target", then a **Build** column of `dotnet build -f …` commands for each target. The running-app rule exists (`verification.md:52`, "A fix does not need a build or a start"), but under "Practical traps", after the table.
- `references/new-app.md:66`: "…and a test, build and exercise it, and only then repeat its pattern."
- `agents/uno-dev.agent.md` and `codex/uno-dev.toml`: the process summary says "build and exercise it".

Other places that lead to a build or restart:

- `uno-testing/SKILL.md:22` and `references/ui.md:25`, step 1: "Build once with `dotnet build`", then "Skip the start if the user already has it running". The build comes before the check for a running app, so an app that is already up gets a build that is wasted or fails on the locked `bin` folder.
- `uno-testing/references/INTERACTION-PATTERNS.md:282`: "Consider restarting the app and test", with no condition and no mention of Hot Reload.
- `verification.md:53` and `ui.md:304`: on MSB3026/MSB3027 the fix is "`uno_app_close`, then build again". That error is the sign the build was not needed.
- `ui.md:289`: "Call `uno_app_close` … before any rebuild" assumes a rebuild will happen.
- `assets/report.template.md:14`: the **Build** row asks for a state and a command. An agent that relied on Hot Reload has nothing to put there, which invites a `dotnet build` to fill it.
- `hooks/hot-reload-edit.sh:59`, the `Failed in` branch: "If no diagnostics are listed below, run dotnet build on the project once", against a running app, which the `uno-testing` rule "One app instance, and close it before you build" forbids.
- `uno-build-app/SKILL.md:69` and `references/checkpoint.md:32`: on resume, "never trust a recorded 'passed' for files that changed". Right for a new session, but it does not check first whether the app is still running.
- `uno-mvux/SKILL.md:25` ("check the generated code against them before finishing") and `uno-build-app/SKILL.md:54-56` ("generated wrapper", "inspect generated output"): the generated ViewModel is on disk only after a build, so an agent that takes this literally runs `dotnet build` against the running app. Low priority; the domain skills are otherwise free of build or restart wording.

Where the don't-build guidance lives:

- `uno-testing` (`SKILL.md` rules and `references/ui.md`), loaded only when the agent is testing.
- `verification.md:52`, under "Practical traps", after the build table.
- `hooks/hot-reload-edit.sh`, which speaks only after an edit, and not at all on Codex.

Builds and restarts that are correct and stay as they are: the baseline before the first edit (`discovery.md:51`, `new-app.md:37`), the persistence check's close and relaunch (`verification.md:33`), Release builds (`verification.md:64-66`, `architecture-baseline.md:71`), and rebuilds without the App MCP (`without-app-mcp.md:60`), where there is no Hot Reload.

## Changes

Three changes, in priority order. The first is the only one that reaches every host.

### 1. Skill text: one rule and the corrections

Every edit lands in both `skills/` and `plugins/uno-platform-studio/skills/`; `Test-Plugin.ps1` fails when they differ.

- **New rule in "Rules that do not bend"** (`uno-build-app/SKILL.md`): "While the app runs under the App MCP, an edit to an existing `.xaml` or `.cs` file is checked with `uno_app_hot_reload_status(file, waitForMs: 10000)`, not `dotnet build`. Build or restart only for a reported RudeEdit or internal error, or a change in the restart list." The restart list is the one already in the hook's reminder; keep one wording in both places.
- **Step 6, rule 4 and `new-app.md:66`:** say that "build" means the first build before a start, then Hot Reload status for each edit after it.
- **Trivial edit row (`SKILL.md:42`):** "Make the edit; with a running app confirm it through Hot Reload status, otherwise build."
- **`references/verification.md`:** move the existing trap at line 52 ("A fix does not need a build or a start") up to sit under the target table, and say there that the **Build** column applies before a start; with a running app, a passing Hot Reload status is the build. Do not add a second copy.
- **`uno-testing/SKILL.md` step 1 and `ui.md` Quick Start step 1:** check `uno_app_get_runtime_info` first; build and start only when no instance is connected.
- **MSB3026/MSB3027 (`verification.md:53`, `ui.md:304`):** first ask whether the build was needed (as `ui.md:94` does); close and build only for a start the rules allow.
- **`ui.md:289`:** "before a start the skill's rules allow" in place of "before any rebuild".
- **`INTERACTION-PATTERNS.md:282`:** replace the restart step with: check `uno_app_hot_reload_status` for the last edited file, and restart only for what it reports.
- **`assets/report.template.md:14`:** accept "Hot Reload status: applied" as evidence for the **Build** row on a running app.
- **Resume (`SKILL.md:69`, `checkpoint.md:32`):** check `uno_app_get_runtime_info` before rebuilding to re-run a check.
- **Generated code (`uno-mvux/SKILL.md:25`, `uno-build-app/SKILL.md:56`):** say that the generated code to read is the one in `obj/` from the last build, and that a compile failure in a Model surfaces in the Hot Reload status; a fresh build to regenerate it is a restart and follows the restart rule.
- **Agent text:** change "build and exercise it" in both `agents/uno-dev.agent.md` and `codex/uno-dev.toml`, and add the new rule to the list of rules they name. `Test-Plugin.ps1` fails when the two differ.

### 2. SessionStart hook, only in Uno projects

- New `hooks/session-start.sh`, registered in `hooks/hooks.json` under `SessionStart` with matcher `startup|resume|clear|compact`, so the rule comes back after a compaction.
- Gate: print nothing unless the project is an Uno app: `global.json` names `Uno.Sdk` under `msbuild-sdks`, or a `.csproj` within 3 levels declares `Sdk="Uno.Sdk"` (with or without a version). Both checks are needed: `discovery.md:16` finds the SDK in either place.
- Output: `additionalContext` of 2 to 3 sentences, the same rule as change 1, plus: check `uno_app_get_runtime_info` before the first build, so an app that is already running is found. No other content.
- Budget: under 400 characters, and a time limit of 5 s.

### 3. Fixes in `hooks/hot-reload-edit.sh`

- When no `.sln` or `.slnx` is found above the edited file, call `remind` instead of `exit 0`. Today the agent gets no word at all in that case.
- In the `Failed in` branch, replace "run dotnet build on the project once to see them" with "call `uno_app_hot_reload_status(file, waitForMs: 10000)` to see them". A build against the running app fails on the locked `bin` folder and contradicts `uno-testing`.
- Align the `remind` text's restart list with the running-app rule (it lacked `InternalError` and the uncovered edit).
- Edits made through the shell (`sed -i`, heredocs) never match `Edit|Write|MultiEdit`. Decision: no `Bash` matcher. Finding the write target in arbitrary shell is neither cheap nor reliable, and change 2 puts the rule in context for those sessions.

## Non-goals

- No rule added to every session. Non-Uno sessions pay nothing, in line with the context cuts in #194.
- No Flutter-style `rules/` folder. Claude Code and Codex would not load it.
- No change to the DevServer, the App MCP or `uno-devserver` CLI behaviour.
- No moving of `uno-testing` content into `uno-build-app` beyond the one rule.
- No change to the builds and restarts that are correct (listed under Root cause): baseline, persistence check, Release, no App MCP.

## Verification

- [x] `.github/scripts/Test-Hooks.ps1` covers the new no-solution path in `hot-reload-edit.sh` (expects the reminder).
- [x] `Test-Hooks.ps1` checks that the `Failed in` reason no longer says `dotnet build` (the `failed` CLI stub at line 42 already drives that branch; add the assertion on `reason`).
- [x] `grep -rniE 'build and exercise|then build again|before any rebuild|restarting the app' skills plugins`: no matches remain.
- [x] `.github/scripts/Test-Plugin.ps1` passes (skill mirror and agent Markdown/Codex parity).
- [x] `Test-Hooks.ps1` covers `session-start.sh`: Uno `global.json` and Uno.Sdk-on-project folders print the context; a non-Uno folder (with an Uno `.csproj` under `obj/`) and a missing folder print nothing; output under 400 characters (358).
- [x] `claude plugin validate plugins/uno-platform-studio --strict` passes.
- [ ] Eval: feature mode on a running sample app, three successive `.xaml` and `.cs` edits. Pass = no `dotnet build`, `uno_app_close` or `uno_app_start` between them unless the status reported RudeEdit.
- [ ] Same eval on Codex, where only change 1 applies.
- [ ] Same eval for a trivial one-line edit.
- [ ] Eval: the app is already running when the session starts. Pass = no `dotnet build` or `uno_app_start` before the first edit.

Done when every box is ticked and the eval passes on both hosts.

## Open questions

- Does Codex's plugin format support hooks yet? If so, port change 2 there.
- Which eval suite hosts the edit-then-verify scenarios?

## Sources

- [Flutter AI tools: Agent rules](https://docs.flutter.dev/ai/tools#agent-rules)
- [flutter/agent-plugins rules folder](https://github.com/flutter/agent-plugins/tree/main/rules)
- [Claude Code plugin manifest reference](https://code.claude.com/docs/en/plugins-reference)
