---
name: uno-build-app
description: "End-to-end workflow for building Uno Platform apps and features: discovery, a short blueprint, one complete feature built and exercised before its pattern is repeated, and an honest completion report. Use when creating a new Uno Platform app from a brief, adding a feature to an existing Uno app, repairing a broken Uno page or feature, or diagnosing an Uno binding, state or navigation defect. Use it for any 'how should I implement this' feature request that spans a page, its state, storage or a platform API such as a file or photo picker. Not for WPF, WinUI-only or .NET MAUI projects. For a single control or API question, load the matching domain skill (uno-mvux, uno-navigation, uno-toolkit, uno-themes, uno-authentication, uno-kiota, uno-testing) instead."
compatibility: Works with any agent that can read files and run the .NET CLI. Runtime checks use the Uno App MCP (uno_app_* tools), available UI automation, or existing runtime tests; checks that cannot run are reported as not run.
metadata:
  author: uno-platform
  version: "1.0"
  category: workflow
---

# Uno Platform App-Building Workflow

This skill owns the process. The domain skills (`uno-mvux`, `uno-navigation`, `uno-toolkit`, `uno-themes`, `uno-authentication`, `uno-kiota`, `uno-testing`) supply the mechanics: load only the one the current step needs, and read only the `references/` topic that step calls for, not the whole hub. Evidence, not effort, decides when work is done.

The developer's explicit instructions and the host's permissions always outrank this skill. The developer is the person whose request you are carrying out, never you: a choice this skill leaves to the developer is not yours to make.

## Rules that do not bend

1. **Read before editing.** Read the project's agent instructions (`AGENTS.md`, `CLAUDE.md`, `.github/copilot-instructions.md`), its README and representative source for the area you will touch.
2. **Existing apps keep their architecture, navigation and theme.** Fix only defects the requested work needs, narrowly, and say why. Report other problems; do not repair them.
3. **Ask only when the answer changes** a business rule, roles or access, data ownership, payments, a destructive action, or an external integration contract, and only after checking the brief, assets and project. Keep working on everything that does not depend on the answer. Silence is not approval. Reversible choices use the defaults in the references.
4. **Exercise the first feature in the running app before repeating its pattern.** Build once before the start; after it, the running-app rule.
5. **Never claim a check you did not run.** A check that ran and failed is a failure, not "unavailable".
6. **Views do not do storage or network. Business rules do not reference views. Services are injected.** Code-behind does not decide layout, paging or selection either: the window width switches a layout in XAML (`uno-toolkit`, `references/responsive.md`), more rows come from the paginated feed and the tapped row from the selection state (`uno-mvux`, `references/pagination.md`, `references/selection.md`).
7. **No unrequested packages, SDK, feature or target changes, or migrations.** Never remove a target, an `UnoFeatures` entry or a build property to make a check pass or to simplify a scaffold.
8. **Deliver real behaviour.** If the app presents data as saved, it persists. Do not simulate core behaviour unless the developer asked for a prototype. A missing backend or integration is reported as a gap, never faked.
9. **Automation ids are part of the view.** Every control a person acts on (`Button`, `HyperlinkButton`, `TextBox`, `CheckBox`, `ToggleSwitch`, `ComboBox`, `Slider`, list and tab items, navigation items) and every element a check reads gets `AutomationProperties.AutomationId`, PascalCase, named for its role and unique on its page (`SaveButton`, `SearchBox`, `AssetsList`); inside an item template, bind it to the item's key. The id is never spoken: a page's title and each section or card title carry a `HeadingLevel`, and the name a screen reader speaks (`AutomationProperties.Name`), a tooltip and a shortcut hint are text, and where the app keeps its text in string resources they come from them, in every language ([references/accessibility.md](references/accessibility.md)). Write the id with the element, never as a later pass: the `uno-testing` skill drives the app by `automationId` in one call, with no snapshot, and a control without one costs a snapshot or a coordinate click every time.
10. **Nothing of Studio's over the app.** In an app you created, `App.xaml.cs` calls `MainWindow.UseStudio(showHotReloadIndicator: false, launchHotDesignOnStart: false);` under `#if DEBUG`, before the first start. The indicator and the Hot Design button otherwise sit over the app in every Debug screenshot and under pointer clicks, and Hot Design opens by itself on a first launch. Hot Reload and the App MCP keep working. In an existing app, propose the change and leave the call alone until the developer agrees.
11. **A running app is edited, not rebuilt** (the running-app rule). While the app runs under the App MCP, an edit to an existing `.xaml` or `.cs` file is confirmed with `uno_app_hot_reload_status(file, waitForMs: 10000)`, never with `dotnet build`, `uno_app_close` or `uno_app_start`; its result carries the compiler diagnostics. A start is right only for: launch arguments, platform or app data; a Release build; the `.csproj` or packages; code that already ran at startup (`OnLaunched`, host configuration, DI registrations); and a `RudeEdit`, `InternalError` or uncovered edit the status reported.
12. **Page XAML carries no literal look.** No colour literal, `FontSize`, `FontFamily` or copied `ControlTemplate` in a page you write: pages use the theme's semantic brushes, type-scale styles and control styles. Before the first page's XAML, read [references/design.md](references/design.md), and [references/design-values.md](references/design-values.md) when the brief or design gives its own colours, sizes, typeface or radii: those values go into the theme once. Then load `uno-themes` (`references/semantic-colors-brushes.md`) for the keys. The step that writes pages needs these reads even when no theme work was asked for.
13. **Build for the table, not for the rows you saw.** How few rows the environment holds today, the effort, or "simpler" is never a reason to load a table whole, to keep one in-memory snapshot that every page filters, to show only the records reachable from another list, or to put a `ComboBox` where the user picks a row of another table. Each list reads its own table through a paginated feed, with search, filter and sort sent to the server. A total, a count or a signal "computed from the records" is asked of the server (a count or an aggregate request), not computed from rows loaded for it. A field that points at another record opens a picker that searches the server: every such field on every form, a custom table's included. A currency is data too: an amount shows its record's currency and a total the currency it was summed in, both read from the backend, never a symbol or code typed into a page, a resource or a formatter because every row you saw had it. Writing a departure down does not make it allowed: a departure from a skill rule is something you tried that failed here, reported with the failure, or a decision the developer made.
14. **A desktop window is resizable: desktop-only is not wide-only.** Every page has a narrow layout, written with the page and not as a later pass, whatever sizes the brief's mock-ups show: [references/design.md](references/design.md), *Window sizes*. Nothing in a page, pane, dialog, picker, flyout, tab bar or form block asks for more than about 320 of width, and no bar of tabs, filters or commands scrolls sideways to fit. In a narrow window every record page, not only the first, keeps Back, Save and Discard in its command bar and moves the rest into a More menu. A screenshot of each page in a narrow window (about 400 wide) is part of its visual acceptance: read it, and a title, tab label or figure cut off at an edge is a defect to fix, not a note.
15. **A list and a form are finished whole, every one of them.** Each list, not only the main ones (a dashboard's lists, a record's related lists, a picker, a flyout): a loading state; an empty state that says what is missing and offers the add action; a no-match state that names the search and offers Clear, where the list can be searched; an error state that says what failed, with Retry. A table-like list has column headers, and a header or a sort menu sends the sort to the server. Each form and record page: F5 reloads, Escape closes the pane, dialog or picker, Ctrl+S saves; a Save that validation refuses lists the errors and moves focus to the first invalid field; leaving with unsaved changes asks first. Contracts: [references/recipes.md](references/recipes.md), `search-filter-results` and `edit-form`. Templates: `uno-mvux`, `references/feedview.md`.
16. **What one table's pages do, every table's pages do, and a later feature never removes an earlier one.** Create, edit and delete, related lists on the record, and rule 15's list and form behaviour apply to every table that has a page, unless the brief or the backend rules one out. Keep a grid in the checkpoint ([assets/checkpoint.template.md](assets/checkpoint.template.md) has it), one row per table and one column per capability (search, sort, paging, list states, form fields, pickers, related lists, create, edit, delete, unsaved-changes guard, keyboard, narrow layout); start it with the first table's pages, fill each row from the code when that table's pages are written, and again before the report. A cell that was filled and is now empty is a regression to repair, not a decision to record.

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
| A typo, a resource value, a one-line fix | Trivial edit | Make the edit; with the app running, confirm it through Hot Reload status (the running-app rule), otherwise build. No blueprint, no checkpoint, no report template |

Feature, Repair and Scoped visual modes follow [references/existing-app.md](references/existing-app.md). Every mode but New app starts with [references/discovery.md](references/discovery.md), scaled to the request; a new app has nothing to discover yet, and [references/new-app.md](references/new-app.md) has its own baseline step.

## Step 2: the phases

Agent guidance, not a state machine. Scale each phase to the request.

1. **Inspect and scope.** Discovery per [references/discovery.md](references/discovery.md); in New app mode, the brief and assets, then [references/new-app.md](references/new-app.md). Output: target project, mode, applicable instructions, target matrix, and the build and test failures that already exist before you change anything.
2. **Blueprint** (New app only). Fill in [assets/blueprint.template.md](assets/blueprint.template.md) after scaffolding and before writing any app source, following [references/new-app.md](references/new-app.md) and [references/architecture-baseline.md](references/architecture-baseline.md). Keep it current when decisions change. Before filling it in, load `uno-mvux` (`references/overview.md`) and `uno-navigation` (`references/setup.md`, `references/routes.md`): the default stack's models and routes are written with them.
3. **Feature contract.** For each meaningful feature, write down: trigger and result; who owns the state and the service; navigation in and out, with payload; data source and persistence; loading, empty and error states; theme, responsive and accessibility states affected; the checks that will prove it. A list's contract names its table, the server query behind its search, sort and paging, the request its counts and totals come from, and the texts of its empty and no-match states. A form's contract names where its field list and field rules come from, and which fields pick a row of another table. Every page's contract names its narrow layout. Identify entity identity, invariants and invalid operations from the requirements; never invent missing business rules. "Use `IState<bool>`" is not a contract. "Toggling persists through the settings service, shows a recoverable save failure, and is restored after relaunch" is.
4. **Select a recipe and skills.** Match the contract to [references/recipes.md](references/recipes.md). Load only the domain skills this feature needs, and within each only the references it names. An incompatible recipe still lends its acceptance checks.
5. **Implement one complete feature.** Pick the slice that exercises the risky pattern (settings persistence beats another static page). Do rule 12's reads before the first page's XAML. Wire view, model, service, registration, navigation and a meaningful test before replicating anything. If a generated command does nothing, diagnose it (signature, parameters, binding context, generated wrapper, logs) before adding a code-behind workaround. A rendered button with no observable effect is unfinished work. A property changed through live tooling is not a source fix.
6. **Build once, start, then exercise.** Every task-relevant target that can run on this machine, per [references/verification.md](references/verification.md); after the start, each edit is confirmed through Hot Reload status (the running-app rule), not another build. A screenshot proves rendering, not persistence. Visual quality is checked separately from function, per [references/design.md](references/design.md).
7. **Recover.** Read diagnostics before changing approach. After two materially equivalent failed repairs, change strategy: reduce to a reproducer, inspect generated output (the `obj/` of the last build; a Model that fails to compile shows in the Hot Reload status, and a build to regenerate it is a restart under the running-app rule), consult version-matched docs, or report the blocker. When docs, skills and observed behaviour disagree, what you verified on the project's exact versions wins; record the versions and do not upgrade anything to escape the conflict.
8. **Repeat and finish.** Reuse the verified pattern. If runtime checks were unavailable but the build and service tests passed, you may reuse it provisionally; record the pending checks per feature. Never repeat a pattern with an observed, unresolved runtime failure. Re-check across features when shared state, theme or navigation changed. Compare every requested journey and visible action against what now works, and fill rule 16's grid from the code: what an earlier feature had and the app has lost is repaired before the report. For a new app, go through [references/finish.md](references/finish.md). Then report.

## Completion report

Fill in [assets/report.template.md](assets/report.template.md). It separates what ran, what failed, what could not run and what was waived, per target, and lists implementation gaps separately from verification gaps. Its closing line is exactly one of the three the template gives. A failed check demonstrating a defect in the requested behaviour means **"Implementation incomplete."** When no implementation gap is known but required verification failed, could not run or is pending, use **"Implementation complete; verification incomplete."**, followed by the checks, targets and reasons. Do not claim verification is complete in that case.

Trivial edit and Diagnostic modes skip the template: say in one or two sentences what changed or what you found, and what you ran to check it. The honesty rules still apply; name any check you could not run.

Only an explicit instruction from the developer, or a standing project policy, waives a required check. Report a waived check as waived, with who waived it, never as passed.

## Checkpoints and handoff

Substantial work (a new app, several features, or anything likely to span sessions) leaves a checkpoint, updated at milestones, per [references/checkpoint.md](references/checkpoint.md). Trivial edits do not. On request, produce a handoff note another agent or machine can resume from. When resuming, re-read the source and re-check tools, starting with `uno_app_get_runtime_info`: an app that is still running takes the re-check through Hot Reload, not a build. Never trust a recorded "passed" for files that changed.

## References

| File | Read when |
|---|---|
| [references/discovery.md](references/discovery.md) | Every mode but New app, at the start |
| [references/new-app.md](references/new-app.md) | New app mode: template command, default stack, targets, blueprint |
| [references/architecture-baseline.md](references/architecture-baseline.md) | Designing a new app or a substantive feature |
| [references/existing-app.md](references/existing-app.md) | Feature, Repair and Scoped visual modes |
| [references/design.md](references/design.md) | Before the first page's XAML; any theme, colour, typography or layout work; visual acceptance |
| [references/design-values.md](references/design-values.md) | The brief or design gives its own colours, font sizes, typeface, radii or spacing |
| [references/accessibility.md](references/accessibility.md) | Writing a page: names, headings, live regions and landmarks for assistive technology |
| [references/verification.md](references/verification.md) | Building, testing and exercising each target; failure attribution |
| [references/recipes.md](references/recipes.md) | Choosing an implementation pattern and its acceptance checks |
| [references/sample-data.md](references/sample-data.md) | The brief asks for offline use, sample data or a demo mode beside a live backend |
| [references/finish.md](references/finish.md) | A new app, before the report: app identity, icon and splash, template leftovers, unhandled failures, Release build |
| [references/checkpoint.md](references/checkpoint.md) | Substantial work, handoff, resuming |

| Template | Produces |
|---|---|
| [assets/blueprint.template.md](assets/blueprint.template.md) | The new-app blueprint |
| [assets/report.template.md](assets/report.template.md) | The completion report |
| [assets/checkpoint.template.md](assets/checkpoint.template.md) | The task checkpoint |
| [assets/handoff.template.md](assets/handoff.template.md) | The portable handoff note |
