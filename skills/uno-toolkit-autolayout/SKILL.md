---
name: uno-toolkit-autolayout
description: "AutoLayout for Figma-like layouts: rows or columns with uniform spacing, alignment, and padding. Use when mirroring Figma Auto-Layout or converting Figma designs to XAML, using space-between distribution, or per-child alignment overrides (CounterAlignment). Arranges children with Figma-like spacing, alignment, and padding."
metadata:
  author: uno-platform
  version: "2.4"
  category: toolkit
---

# Uno Toolkit AutoLayout — Agent Skill

## Workflow

> **Docs lookup:** call `uno_platform_docs_search(...)` first, then `uno_platform_docs_fetch(sourcePath="…")` using the `sourcePath` field from a result (a relative `.md` path; add the result's `anchor` for a section). Never pass a URL, a `.html` link, or a hand-built path.

### Step 1: Fetch the AutoLayout Documentation

```
uno_platform_docs_search("Uno Toolkit AutoLayout panel Figma spacing alignment")
```

Primary documentation page:
- **AutoLayout Control**: `external/uno.toolkit.ui/doc/controls/AutoLayoutControl.md`

Fetch the page:

```
uno_platform_docs_fetch(sourcePath="external/uno.toolkit.ui/doc/controls/AutoLayoutControl.md")
```

### Step 2: For How-To Walkthroughs

```
uno_platform_docs_search("AutoLayout howto walkthrough example Figma")
```

## Key Principles (Stable)

**Container properties** (set on the `<utu:AutoLayout>` element itself):

- `Orientation` — Horizontal or Vertical (primary axis)
- `Spacing` — gap between children (like Figma auto-layout spacing)
- `Justify` — SpaceBetween distribution
- `PrimaryAxisAlignment` — alignment on the primary axis (`Start`, `Center`, `End`, `Stretch`)
- `CounterAxisAlignment` — alignment on the cross axis (`Start`, `Center`, `End`, `Stretch`)
- `Padding` — Figma-anchor-style padding (`Thickness`)

**Per-child attached properties** (set on children of an `AutoLayout`, **note: NOT the same names as the container properties**):

- `utu:AutoLayout.PrimaryAlignment` — override on primary axis (`Auto` or `Stretch`)
- `utu:AutoLayout.CounterAlignment` — override on counter axis (`Start`, `Center`, `End`, `Stretch`) — **`CounterAlignment`, NOT `CounterAxisAlignment`**
- `utu:AutoLayout.PrimaryLength` — fixed size along orientation axis (`double`)
- `utu:AutoLayout.CounterLength` — fixed size perpendicular to orientation (`double`)
- `utu:AutoLayout.IsIndependentLayout` — absolute positioning (`bool`)

There is **no** `AutoLayout.Counterpart`, no `AutoLayout.SetCounterAxisAlignment(...)`, no `AutoLayout.Align`, no `AutoLayout.MainAxis`, no `AutoLayout.CrossAxis`. If unsure, the canonical surface is the table above — do not invent member names.

**XAML namespace:** `xmlns:utu="using:Uno.Toolkit.UI"`

**Not AutoLayout's job:** if the layout is described in CSS terms (`flex-grow` / `flex-shrink` / `flex-basis`, `justify-content`, `align-items`) or the children must **wrap** onto multiple lines — which `AutoLayout` cannot do — use `FlexPanel` instead.

## Related Skills

- [[uno-toolkit-flexpanel]] — CSS Flexbox layout (wrapping, grow/shrink/basis)
- [[uno-toolkit-responsive]] — Responsive layout based on screen size
