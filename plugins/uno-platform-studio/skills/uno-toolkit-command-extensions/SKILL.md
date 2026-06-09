---
name: uno-toolkit-command-extensions
description: "Attached properties (`CommandExtensions.Command`, `CommandExtensions.CommandParameter`) that bind controls without a built-in `Command` to MVVM commands."
when_to_use: "Use when a UI event (Enter in `TextBox`, item click in `ListView`, `ToggleSwitch` toggle, `NavigationViewItem` invocation, any `UIElement` tap) must invoke an MVVM command without code-behind event handlers."
metadata:
  author: uno-platform
  version: "2.5"
  category: toolkit
---

# Uno Toolkit Command Extensions — Agent Skill

## Workflow

> **Docs lookup:** call `uno_platform_docs_search(...)` first, then `uno_platform_docs_fetch(sourcePath="…")` using the `sourcePath` field from a result (a relative `.md` path; add the result's `anchor` for a section). Never pass a URL, a `.html` link, or a hand-built path.

### Step 1: Fetch the CommandExtensions Documentation

```
uno_platform_docs_search("Uno Toolkit CommandExtensions command TextBox enter ToggleSwitch ListView tap")
```

Primary documentation pages:
- **CommandExtensions (Chefs)**: `external/uno.chefs/doc/toolkit/CommandExtensions.md`
- **Command Extensions helper**: search for `CommandExtensions` in toolkit helpers

Fetch the Chefs page:

```
uno_platform_docs_fetch(sourcePath="external/uno.chefs/doc/toolkit/CommandExtensions.md")
```

### Step 2: For Reference Documentation

```
uno_platform_docs_search("Uno Toolkit CommandExtensions attached property Command CommandParameter")
```

## Critical Rules

- There is **no `CommandTrigger` attached property**. `utu:CommandExtensions.Command="{Binding ...}"` alone is sufficient — the trigger event is determined automatically by the control type (Enter on `TextBox`/`PasswordBox`, item click on `ListView`, toggle on `ToggleSwitch`, invocation on `NavigationView`, tap on any `UIElement`). Do not invent a separate trigger property.
- `Command` on `ListView` requires `IsItemClickEnabled="True"` to fire.

## Key Principles (Stable)

- `utu:CommandExtensions.Command="{Binding MyCommand}"` — attaches a command
- `utu:CommandExtensions.CommandParameter` — optional parameter
- Supported controls: TextBox (enter key), PasswordBox, ToggleSwitch, ListView, NavigationView, ItemsRepeater, any UIElement (tap)
- XAML namespace: `xmlns:utu="using:Uno.Toolkit.UI"`

## Related Skills

- [[uno-toolkit-input-extensions]] — Input field focus flow
- [[uno-toolkit-itemsrepeater-extensions]] — ItemsRepeater selection
