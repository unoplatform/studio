---
name: uno-toolkit-input-extensions
description: "Use InputExtensions for form input UX improvements including auto-focus next field, auto-dismiss keyboard, and custom return key types."
when_to_use: "Use when implementing forms that auto-advance focus on Enter, dismiss the keyboard on completion, or set platform-specific return key types (Next, Done, Search) — typically login forms or multi-field input sequences on mobile."
metadata:
  author: uno-platform
  version: "2.4"
  category: toolkit
---

# Uno Toolkit Input Extensions — Agent Skill

## Workflow

> **Docs lookup:** call `uno_platform_docs_search(...)` first, then `uno_platform_docs_fetch(sourcePath="…")` using the `sourcePath` field from a result (a relative `.md` path; add the result's `anchor` for a section). Never pass a URL, a `.html` link, or a hand-built path.

### Step 1: Fetch the InputExtensions Documentation

```
uno_platform_docs_search("Uno Toolkit InputExtensions auto focus next dismiss keyboard return type")
```

Primary documentation pages:
- **Input Extensions**: `external/uno.toolkit.ui/doc/helpers/Input-extensions.md`
- **Input Extensions How-To**: `external/uno.toolkit.ui/doc/helpers/walkthroughs/input-extensions.howto.md`

Fetch the helper reference:

```
uno_platform_docs_fetch(sourcePath="external/uno.toolkit.ui/doc/helpers/Input-extensions.md")
```

### Step 2: For Login Form Example

```
uno_platform_docs_fetch(sourcePath="external/uno.toolkit.ui/doc/helpers/walkthroughs/input-extensions.howto.md")
```

## Key Principles (Stable)

- `utu:InputExtensions.AutoFocusNext="True"` — moves focus to next field on Enter
- `utu:InputExtensions.AutoDismiss="True"` — dismisses keyboard on last field Enter
- `utu:InputExtensions.ReturnType="Next|Done|Search|Send|Go"` — mobile keyboard button
- Essential for login forms and any multi-field input
- XAML namespace: `xmlns:utu="using:Uno.Toolkit.UI"`

## Related Skills

- [[uno-toolkit-safearea]] — Keyboard-aware safe area for forms
- [[uno-toolkit-command-extensions]] — Command on TextBox enter
