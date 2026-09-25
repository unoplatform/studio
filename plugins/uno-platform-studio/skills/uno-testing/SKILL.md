---
name: uno-testing
description: "Automated UI testing and verification of a running Uno Platform app through the Uno App MCP (the uno_app_* tools): start the app, take a visual-tree snapshot, find elements by x:Name or AutomationId, click, type, press keys, scroll, drive navigation flows, capture screenshots, read an element's DataContext, and assert on text, visibility, enabled state, list counts, dialogs, layout, and FeedView loading/error/value states. Use whenever the user says check that, verify, test the UI, does it render, click through, make sure the button works, take a screenshot, why is X not visible, or asks for end-to-end or visual regression testing of an Uno app. Also use after any UI change you made when the uno_app_* tools are available, so the result is confirmed in the real app rather than assumed from the code."
compatibility: Requires the Uno App MCP server (the uno_app_* tools, provided by the Uno tooling) to be configured and available. Works with Uno Platform applications targeting desktop (Windows, macOS, Linux), WebAssembly, iOS, and Android.
metadata:
  author: uno-platform
  version: "3.0"
  category: testing
---

# Uno Testing

The Uno App MCP lets you drive the real running app: launch it, read its visual tree, act on elements through automation peers, and check what actually rendered. Use it to prove a change works instead of inferring from source. The failure modes here are mostly timing and stale references, so the rules below are about when to refresh and when to wait.

## Prerequisite

Confirm the `uno_app_*` tools exist before starting. If they are absent, say so plainly and tell the user to enable the Uno tooling; then fall back to static review of the XAML and Model code, labelled as unverified. Do not describe test results you did not obtain.

## Workflow

1. **Start and confirm.** `uno_app_start(projectPath, targetFramework)` with the `.csproj` path and a TFM such as `net9.0-desktop` or `net9.0-browserwasm`, then `uno_app_get_runtime_info` to confirm it is up. Skip the start if the user already has it running.
2. **Baseline.** `uno_app_get_screenshot` plus `uno_app_visualtree_snapshot(justMyCode: true)` before touching anything, so failures have a before/after.
3. **Locate.** Find the target in the snapshot by `AutomationId`, then `x:Name`, then `Content`/`Text`, then type-and-position as a last resort. Take the `handle`.
4. **Act.** Prefer `uno_app_element_peer_default_action(elementRef)`; use `uno_app_element_peer_action` for specific patterns, `uno_app_type_text` after focusing an input, `uno_app_key_press` for Tab/Enter/Escape/arrows, and `uno_app_pointer_click` with `includeBounds: true` coordinates only when peers do not apply.
5. **Wait, then re-snapshot.** Poll the tree until loading indicators are gone or the expected element appears (about 10 tries, 500 ms apart). Handles from the previous snapshot are invalid after UI changes.
6. **Assert.** Check the tree for presence/absence and properties (`Text`, `Content`, `IsEnabled`, `IsChecked`, `SelectedIndex`), and `uno_app_get_element_datacontext(elementRef)` for ViewModel state. Screenshot the final state.
7. **Report and clean up.** State each assertion with expected vs actual, attach screenshots, and `uno_app_close` on desktop unless the user wants the app left open. On failure, also capture a full tree (`justMyCode: false`) and the DataContext.

## Topic map

| Task | Read |
|------|------|
| Tool catalogue, parameters, lifecycle, and the four base test patterns (click, form, navigation, visual regression) | `references/ui.md` |
| Assertion patterns: page loaded, button state, text, list counts, dialogs, validation errors, navigation, wait/retry loops, failure diagnostics | `references/assertions.md` |
| Interaction recipes: text entry and clearing, keyboard navigation, lists and multi-select, ComboBox, sliders, dialogs, scrolling, context menus | `references/INTERACTION-PATTERNS.md` |
| Reading the snapshot XML, `justMyCode`/`includeBounds`/`includeHidden`, element-finding strategies, recycled list handles | `references/VISUAL-TREE-GUIDE.md` |

## Critical rules

- **Handles expire.** Any action that changes UI invalidates earlier handles; always re-snapshot before the next interaction. Recycled `ListView` items change handles on scroll.
- **Never assert during a transient state.** A `ProgressRing`, `ProgressBar`, loading text, or disabled submit button means the async work is still running; poll until it clears, then assert. Asserting early produces false failures and false passes.
- **Peers before coordinates.** Automation peers survive layout changes and DPI differences; coordinate clicks do not. When coordinates are unavoidable, request `includeBounds: true` in the same snapshot and click the centre of the bounds.
- **`AutomationId` over generated names.** It is stable across refactors; `x:Name` is next best. If neither exists and the test matters, suggest adding `AutomationProperties.AutomationId` to the XAML.
- **Keep `justMyCode: true`** unless debugging templates; the full tree is large and noisy. Use `includeHidden: true` only to prove something is collapsed, never to interact with it.
- **Be specific.** Assert exact text, counts, and enabled states rather than "element exists". Absence is asserted with `includeHidden: false`.
- **Screenshot before and after.** Evidence on both pass and fail is what makes the report reviewable and makes regressions diagnosable later.
- **Feed states are three assertions, not one.** For a `FeedView`, check the progress template appears, then the value or error template replaces it, then the bound data matches via DataContext.

## Related skills

- `uno-mvux` for what the loading, error, and value states of a `FeedView` should contain and how the ViewModel exposes them.
- `uno-navigation` for the routes and regions a navigation test should land on.
- `uno-toolkit` for the control types (`TabBar`, `NavigationBar`, `CardContentControl`) you will see in the visual tree.
