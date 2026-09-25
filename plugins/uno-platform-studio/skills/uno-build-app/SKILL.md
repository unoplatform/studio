---
name: uno-build-app
description: "End-to-end workflow for building Uno Platform apps and features: discovery, a short blueprint, one complete feature built and exercised before its pattern is repeated, and an honest completion report."
when_to_use: "Use when creating a new Uno Platform app from a brief, adding a feature to an existing Uno app, repairing a broken Uno page or feature, or diagnosing an Uno binding, state or navigation defect. Not for WPF, WinUI-only or .NET MAUI projects. For a single control or API question, load the focused uno-* skill instead."
compatibility: Works with any agent that can read files and run the .NET CLI. Runtime checks use the Uno App MCP (uno_app_* tools) when it is available; without it the workflow still runs and reports runtime checks as not run.
metadata:
  author: uno-platform
  version: "1.0"
  category: workflow
---

# Uno Platform App-Building Workflow

This skill owns the process. The focused skills (`uno-mvux-*`, `uno-navigation-*`, `uno-toolkit-*`, `uno-themes-*`, `uno-testing-*`) supply the mechanics: load only the one the current step needs, not all of them. Evidence, not effort, decides when work is done.

The developer's explicit instructions and the host's permissions always outrank this skill.

## Rules that do not bend

1. **Read before editing.** Read the project's agent instructions (`AGENTS.md`, `CLAUDE.md`, `.github/copilot-instructions.md`), its README and representative source for the area you will touch.
2. **Existing apps keep their architecture, navigation and theme.** Fix only defects the requested work needs, narrowly, and say why. Report other problems; do not repair them.
3. **Ask only when the answer changes** a business rule, roles or access, data ownership, payments, a destructive action, or an external integration contract, and only after checking the brief, assets and project. Keep working on everything that does not depend on the answer. Silence is not approval. Reversible choices use the defaults in the references.
4. **Build and exercise the first feature before repeating its pattern.**
5. **Never claim a check you did not run.** A check that ran and failed is a failure, not "unavailable".
6. **Views do not do storage or network. Business rules do not reference views. Services are injected.**
7. **No unrequested packages, SDK or target changes, or migrations.** Never remove a target or a build property to make a check pass.
8. **Deliver real behaviour.** If the app presents data as saved, it persists. Do not simulate core behaviour unless the developer asked for a prototype. A missing backend or integration is reported as a gap, never faked.

## Step 1: pick the mode

Classify the request before doing anything else. Only New app mode writes a blueprint.

| Situation | Mode | First action |
|---|---|---|
| Empty directory, "build me an app" | New app | Read brief and assets; check SDK and template availability; write the blueprint |
| Existing app, "add settings" | Feature | Locate current settings, navigation, services and conventions |
| Existing app, "rebuild the X page" | Repair | Repair the existing page in place; never scaffold a replacement |
| Several projects, no obvious target | Discovery | Infer the target from the request; ask only if the choice is consequential |
| "Why does this binding fail?" | Diagnostic | Inspect the narrow path and the generated code; no app plan |
| "Make this card match the design" | Scoped visual | Inspect the card and theme; check the changed states; no service refactor |
| A typo, a resource value, a one-line fix | Trivial edit | Make the edit and build; no blueprint, no checkpoint, no report template |

Feature, Repair and Scoped visual modes follow [references/existing-app.md](references/existing-app.md). Every mode starts with [references/discovery.md](references/discovery.md), scaled to the request.

## Step 2: the phases

Agent guidance, not a state machine. Scale each phase to the request.

1. **Inspect and scope.** Discovery per [references/discovery.md](references/discovery.md). Output: target project, mode, applicable instructions, target matrix, and the build and test failures that already exist before you change anything.
2. **Blueprint** (New app only). Fill in [assets/blueprint.template.md](assets/blueprint.template.md) before the first feature, following [references/new-app.md](references/new-app.md) and [references/architecture-baseline.md](references/architecture-baseline.md). Keep it current when decisions change.
3. **Feature contract.** For each meaningful feature, write down: trigger and result; who owns the state and the service; navigation in and out, with payload; data source and persistence; loading, empty and error states; theme, responsive and accessibility states affected; the checks that will prove it. Identify entity identity, invariants and invalid operations from the requirements; never invent missing business rules. "Use `IState<bool>`" is not a contract. "Toggling persists through the settings service, shows a recoverable save failure, and is restored after relaunch" is.
4. **Select a recipe and skills.** Match the contract to [references/recipes.md](references/recipes.md). Load only the focused skills this feature needs. An incompatible recipe still lends its acceptance checks.
5. **Implement one complete feature.** Pick the slice that exercises the risky pattern (settings persistence beats another static page). Wire view, model, service, registration, navigation and a meaningful test before replicating anything. If a generated command does nothing, diagnose it (signature, parameters, binding context, generated wrapper, logs) before adding a code-behind workaround. A rendered button with no observable effect is unfinished work. A property changed through live tooling is not a source fix.
6. **Build and exercise.** Every task-relevant target that can run on this machine, per [references/verification.md](references/verification.md). A screenshot proves rendering, not persistence. Visual quality is checked separately from function, per [references/design.md](references/design.md).
7. **Recover.** Read diagnostics before changing approach. After two materially equivalent failed repairs, change strategy: reduce to a reproducer, inspect generated output, consult version-matched docs, or report the blocker. When docs, skills and observed behaviour disagree, what you verified on the project's exact versions wins; record the versions and do not upgrade anything to escape the conflict.
8. **Repeat and finish.** Reuse the verified pattern. If runtime checks were unavailable but the build and service tests passed, you may reuse it provisionally; record the pending checks per feature. Never repeat a pattern with an observed, unresolved runtime failure. Re-check across features when shared state, theme or navigation changed. Compare every requested journey and visible action against what now works. Then report.

## Completion report

Fill in [assets/report.template.md](assets/report.template.md). It separates what ran, what failed, what could not run and what was waived, per target, and lists implementation gaps separately from verification gaps. Its closing line is exactly one of the three the template gives. When implementation is done and a required check could not run, the only permitted line is **"Implementation complete; verification incomplete."**, followed by the missing checks, targets and reasons. Never write "complete" or "all platforms verified" in that case.

Trivial edit and Diagnostic modes skip the template: say in one or two sentences what changed or what you found, and what you ran to check it. The honesty rules still apply; name any check you could not run.

Only an explicit instruction from the developer, or a standing project policy, waives a required check. Report a waived check as waived, with who waived it, never as passed.

## Checkpoints and handoff

Substantial work (a new app, several features, or anything likely to span sessions) leaves a checkpoint, updated at milestones, per [references/checkpoint.md](references/checkpoint.md). Trivial edits do not. On request, produce a handoff note another agent or machine can resume from. When resuming, re-read the source and re-check tools; never trust a recorded "passed" for files that changed.

## References

| File | Read when |
|---|---|
| [references/discovery.md](references/discovery.md) | Every mode, at the start |
| [references/new-app.md](references/new-app.md) | New app mode: template command, default stack, targets, blueprint |
| [references/architecture-baseline.md](references/architecture-baseline.md) | Designing a new app or a substantive feature |
| [references/existing-app.md](references/existing-app.md) | Feature, Repair and Scoped visual modes |
| [references/design.md](references/design.md) | Any theme, colour, typography or layout work, and visual acceptance |
| [references/verification.md](references/verification.md) | Building, testing and exercising each target; failure attribution |
| [references/recipes.md](references/recipes.md) | Choosing an implementation pattern and its acceptance checks |
| [references/checkpoint.md](references/checkpoint.md) | Substantial work, handoff, resuming |

| Template | Produces |
|---|---|
| [assets/blueprint.template.md](assets/blueprint.template.md) | The new-app blueprint |
| [assets/report.template.md](assets/report.template.md) | The completion report |
| [assets/checkpoint.template.md](assets/checkpoint.template.md) | The task checkpoint |
| [assets/handoff.template.md](assets/handoff.template.md) | The portable handoff note |
