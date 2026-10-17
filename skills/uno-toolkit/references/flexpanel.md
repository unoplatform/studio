# Uno Toolkit FlexPanel

> **Availability:** `FlexPanel` ships in Uno.Toolkit 11.0.0-dev.142 (Uno.Sdk 7.0.0-dev.77) and is absent from Toolkit 9.1.3 (Uno.Sdk 6.7.x). On an older Toolkit, confirm `utu:FlexPanel` resolves before using it. If it does not, use `AutoLayout` or `Grid` instead (there is no `WrapPanel` in the Toolkit or WinUI; the CommunityToolkit ships one).

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

- **Children can be crushed to `0` — that is Yoga, not CSS.** There is no min-content floor (WinUI cannot report one), so a `TextBlock` can shrink to nothing. Set `Shrink="0"` to keep a child at its natural size, or `FlexMinWidth` / `FlexMinHeight` for an explicit floor.

- **`FlexMinWidth` / `FlexMinHeight` are NOT `FrameworkElement.MinWidth` / `MinHeight`.** The framework properties force `Measure` to return at least *X*; these clamp the flex-resolved slot to at least *X*. The `Flex` prefix is deliberate — both can sit on the same element meaning different things.

- **`Visibility="Collapsed"` maps to `display: none`** — the child contributes no size **and no gap slot**.

- **A gap is a floor, not the final separation.** `JustifyContent="SpaceBetween"` distributes whatever is left over *on top of* `ColumnGap` / `RowGap`. A negative gap is clamped to `0` — use a negative child `Margin` for overlap.

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
| `utu:FlexPanel.Position` | `FlexPositionType` (`Relative`, `Absolute`) | `Relative` | `position` |
| `utu:FlexPanel.Left` / `.Top` / `.Right` / `.Bottom` | `double` | `NaN` | inset properties |

`Grow` and `Shrink` are **ratios**, not lengths: a child with `Shrink="1"` beside a child with `Shrink="0"` absorbs the whole overflow. `Position="Absolute"` removes the child from the flex line and places it against the panel's own edges with the insets, so the panel needs a resolved size.

### Properties read directly off the child

No attached property needed — `Margin`, `Width`, `Height` and `Visibility` participate directly. `Width` / `Height` map to a definite item size (`NaN` means `auto`).

### Enum members that silently do nothing

`FlexAlign` is shared by three properties and carries more members than any one of them honours. Do not bind a picker to `Enum.GetValues`.

| Property | Meaningful members |
|---|---|
| `JustifyContent` | `FlexStart`, `Center`, `FlexEnd`, `SpaceBetween`, `SpaceAround`, `SpaceEvenly` |
| `AlignItems` | `Stretch`, `FlexStart`, `Center`, `FlexEnd`, `Baseline` |
| `AlignContent` | `FlexStart`, `Center`, `FlexEnd`, `Stretch`, `SpaceBetween`, `SpaceAround`, `SpaceEvenly` |
| `AlignSelf` | `Auto` plus everything valid for `AlignItems` |

### Members that do NOT exist — do not invent CSS names

There is **no** `FlexPanel.Order` (CSS `order` is not supported and not planned — reorder the children instead), no `FlexPanel.Flex` shorthand, no `FlexPanel.Gap` (it is `ColumnGap` / `RowGap`), no `FlexPanel.AlignContent` attached property (`AlignContent` is container-only; the per-child one is `AlignSelf`), and no `FlexMaxWidth` / `FlexMaxHeight`, `LayoutDirection`, `AspectRatio`, `Overflow` or `BoxSizing`. `Basis` is points only — percentages are not supported. If unsure, the canonical surface is the tables above — do not invent member names.

`FlexPanel` derives from `Panel`, which exposes only `Background`: there is **no** `BorderBrush` or `CornerRadius`. Wrap it in a `Border` if you need a plain visual border — for a card-like surface, use `CardContentControl` instead (see `references/card.md`).

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

## Docs, for what this page does not cover

Use these when the sections above do not answer the question: search, then fetch only the section you need, with the `anchor` from the search result. A whole page stays in context for the rest of the session.

### Fetch the FlexPanel Documentation

```
uno_platform_docs_search("Uno Toolkit FlexPanel flexbox grow shrink basis wrap gap")
```

Primary documentation pages: **FlexPanel**: `external/uno.toolkit.ui/doc/controls/FlexPanel.md`; **FlexPanel How-To**: `external/uno.toolkit.ui/doc/controls/walkthroughs/FlexPanel.howto.md`. Fetch with `uno_platform_docs_fetch(sourcePath="external/uno.toolkit.ui/doc/controls/FlexPanel.md", anchor="…")`; the notes below are the checked surface.

## Related Skills

- `references/autolayout.md` — Figma-style auto-layout frames
- `references/responsive.md` — Responsive layout based on screen size
