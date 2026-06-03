---
name: uno-toolkit-safearea
description: "Use SafeArea to keep content within visible screen bounds, avoiding notches, status bars, and on-screen keyboards. Essential for mobile forms."
when_to_use: "Use when laying out forms, footers, or bottom navigation that must clear iOS home-indicator, Android gesture bar, device notches, rounded corners, the status bar, or the on-screen keyboard. CRITICAL for any mobile page with a `TextBox` or `PasswordBox`. Apply via the `<SafeArea>` control or the `SafeArea.Insets` attached property."
metadata:
  author: uno-platform
  version: "2.4"
  category: toolkit
---

# Uno Toolkit SafeArea — Agent Skill

## Workflow

> **Docs lookup:** call `uno_platform_docs_search(...)` first, then `uno_platform_docs_fetch(sourcePath="…")` using the `sourcePath` field from a result (a relative `.md` path; add the result's `anchor` for a section). Never pass a URL, a `.html` link, or a hand-built path.

### Step 1: Fetch the SafeArea Documentation

```
uno_platform_docs_search("Uno Toolkit SafeArea insets notch keyboard mobile form")
```

Primary documentation pages:
- **SafeArea Control**: `external/uno.toolkit.ui/doc/controls/SafeArea.md`
- **SafeArea How-To**: `external/uno.toolkit.ui/doc/controls/walkthroughs/SafeArea.howto.md`

Fetch the how-to:

```
uno_platform_docs_fetch(sourcePath="external/uno.toolkit.ui/doc/controls/walkthroughs/SafeArea.howto.md")
```

### Step 2: For Detailed Properties Reference

```
uno_platform_docs_fetch(sourcePath="external/uno.toolkit.ui/doc/controls/SafeArea.md")
```

## Key Principles (Stable)

- **ALWAYS use SafeArea** on pages with TextBox/PasswordBox on mobile — keyboard WILL obscure inputs otherwise
- Two usage modes: `<SafeArea Insets="...">` (control) or `utu:SafeArea.Insets="..."` (attached property)
- `Insets` values: `Left`, `Top`, `Right`, `Bottom`, `SoftInput` (keyboard), or combinations
- `Mode` — `Padding` (default, adds padding) or `InsetMode.Margin` (adds margin)
- For keyboard: use `Insets="SoftInput"` or `Insets="SoftInput,Bottom"`
- XAML namespace: `xmlns:utu="using:Uno.Toolkit.UI"`

## Related Skills

- [[uno-toolkit-input-extensions]] — Auto-focus and keyboard behavior for forms
- [[uno-toolkit-statusbar-extensions]] — Status bar customization
