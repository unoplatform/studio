---
name: uno-toolkit-flipview-extensions
description: "Use FlipViewExtensions to add navigation buttons to FlipView. Auto-shows Previous/Next arrows for desktop."
when_to_use: "Use when a `FlipView` needs Previous/Next arrow buttons on desktop (the default touch swipe is mouse-hostile). Buttons auto-show on desktop and can be styled via attached properties."
metadata:
  author: uno-platform
  version: "2.4"
  category: toolkit
---

# Uno Toolkit FlipView Extensions — Agent Skill

## Workflow

> **Docs lookup:** call `uno_platform_docs_search(...)` first, then `uno_platform_docs_fetch(sourcePath="…")` using the `sourcePath` field from a result (a relative `.md` path; add the result's `anchor` for a section). Never pass a URL, a `.html` link, or a hand-built path.

### Step 1: Fetch the FlipView Extensions Documentation

```
uno_platform_docs_search("Uno Toolkit FlipView extensions navigation buttons previous next arrows")
```

### Step 2: For Reference

```
uno_platform_docs_fetch(sourcePath="external/uno.toolkit.ui/doc/controls-styles.md")
```

Look for FlipViewExtensions in the helpers section.

## Key Principles (Stable)

- Shows Previous/Next navigation arrows on FlipView
- Buttons auto-show on hover for desktop
- Customizable button templates via default chevron styles
- XAML namespace: `xmlns:utu="using:Uno.Toolkit.UI"`

## Related Skills

- (standalone helper, no primary dependencies)
