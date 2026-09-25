---
name: uno-toolkit-flexpanel
description: "Use FlexPanel to arrange children with CSS Flexbox semantics — grow/shrink/basis distribution, wrapping, gaps, and per-item alignment. Use when the layout is described in CSS terms — `flex-grow` / `flex-shrink` / `flex-basis` distribution, `justify-content` / `align-items` / `align-self`, `row-gap` / `column-gap`, `flex-wrap` — or when porting a web layout where matching flexbox exactly matters. ALWAYS use this skill when children must WRAP onto multiple lines (`AutoLayout` cannot wrap), when items must share space by ratio, or when a child must be sized by `Basis` rather than `Width`. For reproducing a Figma auto-layout frame — including independent/absolutely-positioned children, reverse Z-index, or negative spacing — use `uno-toolkit-autolayout` instead."
metadata:
  author: uno-platform
  version: "1.0"
  category: toolkit
---

# Uno Toolkit FlexPanel — Agent Skill

## Workflow

> **Docs lookup:** call `uno_platform_docs_search(...)` first, then `uno_platform_docs_fetch(sourcePath="…")` using the `sourcePath` field from a result (a relative `.md` path; add the result's `anchor` for a section). Never pass a URL, a `.html` link, or a hand-built path.

### Step 1: Fetch the FlexPanel Documentation

```
uno_platform_docs_search("Uno Toolkit FlexPanel flexbox grow shrink basis wrap gap")
```

Primary documentation pages:
- **FlexPanel Control**: `external/uno.toolkit.ui/doc/controls/FlexPanel.md`
- **FlexPanel How-To**: `external/uno.toolkit.ui/doc/controls/walkthroughs/FlexPanel.howto.md`

Fetch the how-to for task-oriented recipes:

```
uno_platform_docs_fetch(sourcePath="external/uno.toolkit.ui/doc/controls/walkthroughs/FlexPanel.howto.md")
```

### Step 2: For Detailed Properties Reference

```
uno_platform_docs_fetch(sourcePath="external/uno.toolkit.ui/doc/controls/FlexPanel.md")
```

## Key Principles (Stable)

`FlexPanel` arranges its children using CSS Flexbox semantics. Layout is computed by a vendored C# port of Meta's Yoga engine, so sizing, wrapping and distribution follow the CSS specification rather than an approximation of it.

**XAML namespace:** `xmlns:utu="using:Uno.Toolkit.UI"`

```xml
<utu:FlexPanel Direction="Row"
               Wrap="Wrap"
               AlignItems="Center"
               ColumnGap="8"
               RowGap="8"
               Padding="16,8">
    <TextBlock Text="MyApp" utu:FlexPanel.Shrink="0" />
    <Border utu:FlexPanel.Grow="1" />
    <Button Content="Settings" />
</utu:FlexPanel>
```

### Critical Rules

- **`Basis` beats `Width` on the main axis.** This is the most common surprise coming from WinUI. `Basis` is the flex base size; `Width` only seeds the measure. If a child arranges at an unexpected size, check whether `Grow` or `Basis` is resolving the slot.

  ```xml
  <!-- Arranges 400 wide, not 200. -->
  <utu:FlexPanel Width="400">
      <Border Width="200" utu:FlexPanel.Basis="100" utu:FlexPanel.Grow="1" />
  </utu:FlexPanel>
  ```

- **`Grow="1"` alone is NOT `flex: 1 1 0`.** It distributes only the *leftover* space, so items with different content stay different sizes. Equal columns need **both** `Grow="1"` and `Basis="0"`.

  ```xml
  <!-- Three columns of exactly the same width, whatever their content. -->
  <utu:FlexPanel ColumnGap="8">
      <Border utu:FlexPanel.Grow="1" utu:FlexPanel.Basis="0" />
      <Border utu:FlexPanel.Grow="1" utu:FlexPanel.Basis="0" />
      <Border utu:FlexPanel.Grow="1" utu:FlexPanel.Basis="0" />
  </utu:FlexPanel>
  ```

- **Rows can overflow — that is correct flexbox.** A flex item does not shrink below its min-content size by default (CSS Flexbox 4.5). Set `FlexMinWidth="0"` (or `FlexMinHeight="0"` in a column) to opt out. `ScrollViewer` and `ScrollView` children already floor at `0`, matching CSS `overflow: scroll`.

- **`FlexMinWidth` / `FlexMinHeight` are NOT `FrameworkElement.MinWidth` / `MinHeight`.** The framework properties force `Measure` to return at least *X*; these clamp the flex-resolved slot to at least *X*. The `Flex` prefix is deliberate — both can sit on the same element meaning different things.

- **`LayoutDirection` is the only right-to-left input.** `FlowDirection` is ignored. Setting **both** `FlowDirection="RightToLeft"` and `LayoutDirection="RightToLeft"` mirrors the layout **twice**, which cancels out. Set `LayoutDirection` only.

- **`Visibility="Collapsed"` maps to `display: none`** — the child contributes no size **and no gap slot**.

- **A gap is a floor, not the final separation.** `JustifyContent="SpaceBetween"` distributes whatever is left over *on top of* `ColumnGap` / `RowGap`. A negative gap is clamped to `0` — use a negative child `Margin` for overlap.

- **`Direction` defaults to `Row`**, which is *not* the zero value of the `FlexDirection` enum: the enum preserves Yoga's numbering, in which `Column = 0`.

### Container properties

Set on the `<utu:FlexPanel>` element itself.

| Property | Type | Default | CSS equivalent |
|---|---|---|---|
| `Direction` | `FlexDirection` (`Row`, `Column`, `RowReverse`, `ColumnReverse`) | `Row` | `flex-direction` |
| `Wrap` | `FlexWrap` (`NoWrap`, `Wrap`, `WrapReverse`) | `NoWrap` | `flex-wrap` |
| `JustifyContent` | `FlexJustify` | `FlexStart` | `justify-content` |
| `AlignItems` | `FlexAlign` | `Stretch` | `align-items` |
| `AlignContent` | `FlexAlign` | `FlexStart` | `align-content` |
| `ColumnGap` | `double` | `0` | `column-gap` |
| `RowGap` | `double` | `0` | `row-gap` |
| `Padding` | `Thickness` | `0` | `padding` |
| `LayoutDirection` | `FlexLayoutDirection` (`Inherit`, `LeftToRight`, `RightToLeft`) | `LeftToRight` | `direction` |

`JustifyContent` applies to the **main** axis, `AlignItems` to the **cross** axis; changing `Direction` swaps which is which. `AlignContent` positions the wrapped **lines** as a group and only does anything when the content actually wraps.

### Per-child attached properties

Set on the *children* of a `FlexPanel`.

| Property | Type | Default | CSS equivalent |
|---|---|---|---|
| `utu:FlexPanel.Grow` | `double` | `0` | `flex-grow` |
| `utu:FlexPanel.Shrink` | `double` | `1` | `flex-shrink` |
| `utu:FlexPanel.Basis` | `double` | `NaN` (= `auto`) | `flex-basis` (points only) |
| `utu:FlexPanel.FlexMinWidth` | `double` | `NaN` (= `auto`) | `min-width` |
| `utu:FlexPanel.FlexMinHeight` | `double` | `NaN` (= `auto`) | `min-height` |
| `utu:FlexPanel.AlignSelf` | `FlexAlign` | `Auto` | `align-self` |
| `utu:FlexPanel.Position` | `FlexPositionType` (`Static`, `Relative`, `Absolute`) | `Relative` | `position` |
| `utu:FlexPanel.Left` / `.Top` / `.Right` / `.Bottom` | `double` | `NaN` | inset properties |

`Grow` and `Shrink` are **ratios**, not lengths: a child with `Shrink="1"` beside a child with `Shrink="0"` absorbs the whole overflow. `Position="Absolute"` removes the child from the flex line and places it against the panel's own edges with the insets, so the panel needs a resolved size.

### Properties read directly off the child

No attached property needed — `Margin`, `Width`, `Height` and `Visibility` participate directly. `Width` / `Height` map to a definite item size (`NaN` means `auto`).

### Enum members that silently do nothing

`FlexAlign` and `FlexJustify` are shared with the underlying engine and carry more members than any one property honours. Do not bind a picker to `Enum.GetValues`.

| Property | Meaningful members |
|---|---|
| `JustifyContent` | `FlexStart`, `Center`, `FlexEnd`, `SpaceBetween`, `SpaceAround`, `SpaceEvenly` |
| `AlignItems` | `Stretch`, `FlexStart`, `Center`, `FlexEnd`, `Baseline` |
| `AlignContent` | `FlexStart`, `Center`, `FlexEnd`, `Stretch`, `SpaceBetween`, `SpaceAround`, `SpaceEvenly` |
| `AlignSelf` | `Auto` plus everything valid for `AlignItems` |

The remaining `FlexJustify` members (`Auto`, `Stretch`, `Start`, `End`) collapse onto `FlexStart` or `FlexEnd`.

### Members that do NOT exist — do not invent CSS names

There is **no** `FlexPanel.Order` (CSS `order` is not supported and not planned — reorder the children instead), no `FlexPanel.Flex` shorthand, no `FlexPanel.Gap` (it is `ColumnGap` / `RowGap`), no `FlexPanel.AlignContent` attached property (`AlignContent` is container-only; the per-child one is `AlignSelf`), and no `FlexMaxWidth` / `FlexMaxHeight`, `AspectRatio`, `Overflow` or `BoxSizing`. `Basis` is points only — percentages are not supported. If unsure, the canonical surface is the tables above — do not invent member names.

`FlexPanel` derives from `Panel`, which exposes only `Background`: there is **no** `BorderBrush` or `CornerRadius`. Wrap it in a `Border` if you need a plain visual border — for a card-like surface, use `CardContentControl` instead (see [[uno-toolkit-card]]).

Pixel snapping follows the inherited `UseLayoutRounding` (`true` by default, rounding to the current `XamlRoot.RasterizationScale`). Set it to `false` if you see sub-pixel drift from double-rounding on a particular target.

## FlexPanel vs AutoLayout

Independent controls — neither replaces the other.

| Use | When |
|---|---|
| `FlexPanel` | You are thinking in CSS terms: `flex-grow` / `flex-shrink` / `flex-basis` distribution, wrapping onto multiple lines, `justify-content` / `align-items`, gaps that act as a floor. Also when porting a web layout, where matching flexbox exactly matters. |
| `AutoLayout` | You are reproducing a Figma auto-layout frame and want its semantics — including independent (absolutely positioned) children, reverse Z-index, and negative spacing. |

Two capability differences worth knowing before choosing:

- `FlexPanel` wraps onto multiple lines; `AutoLayout` does not.
- `AutoLayout` supports negative `Spacing` (for deliberately overlapping items such as stacked avatars). `FlexPanel` cannot — the engine clamps a negative gap to `0`. Use a negative child `Margin` instead.

They also differ on overflow: with every child at `Shrink="0"`, `FlexPanel` **overflows** rather than fitting the content to the panel, where `AutoLayout` would fit them.

## Related Skills

- [[uno-toolkit-autolayout]] — Figma-style auto-layout frames
- [[uno-toolkit-responsive]] — Responsive layout based on screen size
