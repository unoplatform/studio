---
name: uno-toolkit-responsive
description: "Screen-size-based UI adaptation via the `ResponsiveExtension` markup extension and `ResponsiveView` control."
when_to_use: "Use when one layout must adapt to phone, tablet, or desktop, when XAML needs per-breakpoint property values (font size, orientation, padding, margin), or when an entire view subtree differs by screen size. `ResponsiveExtension` swaps property values; `ResponsiveView` swaps entire `DataTemplate`s. Custom breakpoints via `ResponsiveLayout`."
metadata:
  author: uno-platform
  version: "2.5"
  category: toolkit
---

# Uno Toolkit Responsive — Agent Skill

## Workflow

> **Docs lookup:** call `uno_platform_docs_search(...)` first, then `uno_platform_docs_fetch(sourcePath="…")` using the `sourcePath` field from a result (a relative `.md` path; add the result's `anchor` for a section). Never pass a URL, a `.html` link, or a hand-built path.

### Step 1: Fetch the ResponsiveExtension Documentation

```
uno_platform_docs_search("Uno Toolkit ResponsiveExtension responsive screen size breakpoints property")
```

Primary documentation pages:
- **ResponsiveExtension**: `external/uno.toolkit.ui/doc/helpers/responsive-extension.md`
- **ResponsiveView**: `external/uno.toolkit.ui/doc/controls/ResponsiveView.md`

Fetch ResponsiveExtension:

```
uno_platform_docs_fetch(sourcePath="external/uno.toolkit.ui/doc/helpers/responsive-extension.md")
```

### Step 2: For ResponsiveView (Template Switching)

Fetch ResponsiveView for swapping entire layouts:

```
uno_platform_docs_fetch(sourcePath="external/uno.toolkit.ui/doc/controls/ResponsiveView.md")
```

### Step 3: For How-To Examples

```
uno_platform_docs_search("ResponsiveExtension howto walkthrough responsive layout breakpoints")
```

Key page:
- **ResponsiveExtension How-To**: `external/uno.toolkit.ui/doc/helpers/walkthroughs/responsive-extension.howto.md`

## Critical Rules

- **`ResponsiveExtension` must be used as a markup extension** — `{utu:Responsive Narrow=Red, Wide=Blue}` — never as a XAML element (`<utu:ResponsiveExtension .../>`). The XAML engine parses the breakpoint values as strings and performs type conversion through `MarkupExtension.ProvideValue`, which is only available in markup-extension form.
- On Windows UWP targets, `ResponsiveExtension` only works with `string` value properties due to a `MarkupExtension.ProvideValue(IXamlServiceProvider)` limitation. For non-string properties on Windows UWP, declare the values as resources and pass them via `{StaticResource ...}` (see Uno Toolkit docs).

## Key Principles (Stable)

- **ResponsiveExtension** — sets property values per breakpoint (Narrow, Normal, Wide, etc.) via the `{utu:Responsive ...}` markup-extension form
- **ResponsiveView** — swaps entire DataTemplates per breakpoint (used as an element)
- Default breakpoints: Narrowest (150), Narrow (300), Normal (600), Wide (800), Widest (1080)
- Custom breakpoints via `ResponsiveLayout` resource
- XAML namespace: `xmlns:utu="using:Uno.Toolkit.UI"`

## Related Skills

- [[uno-navigation-responsive-shell]] — Responsive navigation shells
