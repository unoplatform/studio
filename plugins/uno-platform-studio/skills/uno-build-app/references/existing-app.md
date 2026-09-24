# Existing Apps

For Feature, Repair and Scoped visual modes. The goal is the requested change, landed in the app's own style, with nothing else moved.

## Extend in place

- **Never scaffold a new project or a replacement app** to add to or fix an existing one. "Rebuild the settings page" means repair the page that exists.
- Re-read a file immediately before editing it. The developer, or another agent, may have changed it since you last looked.
- Keep the change to what was asked. Do not rewrite unrelated pages, rerun a theming pass, or reorganise folders.

## Follow the conventions local to the feature

Conventions are local, not global. Look at how the pages next to the one you are changing are built, and match them:

- **Presentation**: if the neighbouring pages use MVVM (for example CommunityToolkit.Mvvm's `ObservableObject` and `RelayCommand`), the new feature uses MVVM. If they use MVUX, it uses MVUX. Do not add MVUX, Uno.Extensions Navigation or a theme package to an app that does not have them.
- **Navigation**: add routes the way the app already registers them. An app that navigates with `Frame` or code-behind keeps doing so.
- **Theme**: preserve the existing theme family and its initialisation. See [design.md](design.md), *Existing and legacy themes*.
- **Services and persistence**: reuse the storage, settings and network services that already exist. Do not introduce a second persistence mechanism next to the first.
- **Tests**: add tests with the framework the repository already uses.

The acceptance checks for the feature do not change with the architecture. A persisted setting in an MVVM app must survive a relaunch just like one in an MVUX app. Use the same contract from [recipes.md](recipes.md).

## Mixed codebases

Many apps mix patterns: an MVUX app with a code-behind-heavy page, or Uno.Extensions routes next to manual `Frame` navigation. Mixed is a valid state, not a defect to fix.

- Work in the style of the page you are changing. Do not migrate it.
- Do not reproduce a known defect just to match the local pattern. If the page's pattern is what causes the bug you were asked to fix, repair that one path narrowly and explain why.

## Narrow repairs

When the requested work cannot land without fixing something that is already broken:

1. Fix only what the requested work needs.
2. Keep the fix in the same style as the surrounding code.
3. Say in the report what you fixed and why it was necessary.

Everything else you notice (a stale binding on another page, a hard-coded colour, a missing test) goes into the report as a finding. It is not repaired unless the developer asks.

## Dependency changes

Adding a package, a `UnoFeatures` entry, or changing the SDK, target list or theme is a dependency change, even when it would make the feature easier. State the concrete reason and follow the host's approval policy. Prefer an implementation that needs no new dependency.
