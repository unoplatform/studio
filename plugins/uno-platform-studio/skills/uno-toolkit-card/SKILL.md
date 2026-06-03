---
name: uno-toolkit-card
description: "Use CardContentControl for elevated, filled, or outlined card containers."
when_to_use: "ALWAYS use this skill when building any card-like UI — elevated, filled, or outlined containers. ALWAYS use `CardContentControl` — never use `Card` (it has rigid predefined slots that limit layout flexibility). ALWAYS use this skill instead of creating a `Border` used as a card (with `CornerRadius` combined with `Background`, `Padding`, or `BorderBrush`) — `CardContentControl` provides proper styling, elevation, and theming out of the box. Custom card layouts with full control over content via `ContentTemplate`."
metadata:
  author: uno-platform
  version: "3.6"
  category: toolkit
---

# Uno Toolkit CardContentControl — Agent Skill

## Critical Rules

- **NEVER use `Card`** — always use `CardContentControl`. The `Card` control has rigid predefined slots (HeaderContent, SubHeaderContent, AvatarContent, etc.) that constrain layout. `CardContentControl` gives full layout freedom via `ContentTemplate`.
- **NEVER use `Border` with `CornerRadius` to wrap content.** This is the most common Border anti-pattern in generated XAML and the parser flags it as `GRD1102`. Always use `CardContentControl` instead — it provides correct elevation, corner radius, theming, and accessibility automatically.

### Anti-pattern detection (matches `GRD1102`)

If you find yourself writing a `<Border>` that meets **all** of the following, stop and rewrite it as `utu:CardContentControl`:

1. The `Border` has children (it is **not** self-closing).
2. It sets `CornerRadius` to a non-zero value.
3. It sets at least one of: `Background`, `BorderBrush` (with non-zero `BorderThickness`), or non-zero `Padding`.

That's a card. `<Border CornerRadius="8" Background="..." Padding="16">…</Border>` is a card written the wrong way.

```xml
<!-- WRONG: Border-as-card (the parser will report GRD1102) -->
<Border CornerRadius="8"
        Background="{ThemeResource SurfaceBrush}"
        Padding="16">
    <StackPanel Spacing="8">
        <TextBlock Text="Title"/>
        <TextBlock Text="Body"/>
    </StackPanel>
</Border>

<!-- CORRECT: utu:CardContentControl -->
<utu:CardContentControl Style="{StaticResource ElevatedCardContentControlStyle}">
    <utu:CardContentControl.ContentTemplate>
        <DataTemplate>
            <StackPanel Padding="16" Spacing="8">
                <TextBlock Text="Title"/>
                <TextBlock Text="Body"/>
            </StackPanel>
        </DataTemplate>
    </utu:CardContentControl.ContentTemplate>
</utu:CardContentControl>
```

### When `Border` is still correct

`Border` is the right choice for: dividers (`<Border Height="1" Background="..."/>`), thin separators, simple background fills with no rounded corners, image/content clipping with `CornerRadius` alone (no `Background`/`Padding`/`BorderBrush+BorderThickness`), and layout primitives **inside** a `ControlTemplate`. The grader specifically targets the *card-shaped* combination above; plain layout borders are unaffected.

## Workflow

> **Docs lookup:** call `uno_platform_docs_search(...)` first, then `uno_platform_docs_fetch(sourcePath="…")` using the `sourcePath` field from a result (a relative `.md` path; add the result's `anchor` for a section). Never pass a URL, a `.html` link, or a hand-built path.

### Step 1: Fetch the CardContentControl Documentation

```
uno_platform_docs_search("Uno Toolkit CardContentControl elevated filled outlined")
```

Primary documentation page:
- **Card & CardContentControl**: `external/uno.toolkit.ui/doc/controls/CardAndCardContentControl.md`

Fetch the page:

```
uno_platform_docs_fetch(sourcePath="external/uno.toolkit.ui/doc/controls/CardAndCardContentControl.md")
```

### Step 2: For How-To / Walkthrough

```
uno_platform_docs_search("CardContentControl howto walkthrough custom template")
```

Key page:
- **Card How-To**: `external/uno.toolkit.ui/doc/controls/walkthroughs/Card-and-CardContentControl.howto.md`

## Key Principles (Stable)

- **`CardContentControl`** — fully custom layout via `ContentTemplate`. This is the only card control you should use.
- XAML namespace: `xmlns:utu="using:Uno.Toolkit.UI"`
- Do NOT use `Card` — it has predefined slots that limit layout flexibility
- Do NOT use `Border` as a card (`CornerRadius` + `Background`/`Padding`/`BorderBrush`) — the parser reports this as `GRD1102`; use `CardContentControl` instead

## Critical: Content Binding in DataTemplates

When `CardContentControl` is used **inside** an `ItemsRepeater`, `ListView`, or any other `DataTemplate`, you **MUST** set `Content="{Binding}"` on the `CardContentControl`. Without this, the inner `ContentTemplate > DataTemplate` has **no DataContext** and all bindings inside it resolve to null (rendering the card empty/invisible).

### Rules

- **ALWAYS** set `Content="{Binding}"` on `CardContentControl` when it appears inside a `DataTemplate`
- **NEVER** use indexed bindings (e.g. `{Binding Items[0].Label}`) inside card templates — use `ItemsSource` with a nested `ItemsRepeater` instead
- The `ContentTemplate`'s inner `DataTemplate` inherits its DataContext **only** from the `Content` property — if `Content` is not bound, the inner template has no data

### Correct pattern

```xml
<DataTemplate x:DataType="local:MenuItem">
  <utu:CardContentControl Style="{StaticResource FilledCardContentControlStyle}"
                           Content="{Binding}">
    <utu:CardContentControl.ContentTemplate>
      <DataTemplate>
        <TextBlock Text="{Binding Label}" />
      </DataTemplate>
    </utu:CardContentControl.ContentTemplate>
  </utu:CardContentControl>
</DataTemplate>
```

### Wrong pattern (missing Content binding)

```xml
<!-- ❌ WRONG: No Content="{Binding}" — inner DataTemplate has no DataContext -->
<DataTemplate x:DataType="local:MenuItem">
  <utu:CardContentControl Style="{StaticResource FilledCardContentControlStyle}">
    <utu:CardContentControl.ContentTemplate>
      <DataTemplate>
        <TextBlock Text="{Binding Label}" />  <!-- This resolves to null! -->
      </DataTemplate>
    </utu:CardContentControl.ContentTemplate>
  </utu:CardContentControl>
</DataTemplate>
```

## Sizing — `CardContentControl` does NOT stretch by default

`CardContentControl` sizes to its content. It will **not** fill the available width or height of its parent on its own, even inside a `Grid` cell or `StackPanel` slot that has space to spare. If you need the card to take all available space (a hero card, a full-width row, a column-filling pane), set the alignment **explicitly** on the `CardContentControl` itself:

```xml
<!-- Card fills its grid cell horizontally and vertically -->
<utu:CardContentControl Grid.Row="0"
                        Style="{StaticResource ElevatedCardContentControlStyle}"
                        HorizontalAlignment="Stretch"
                        VerticalAlignment="Stretch">
    <utu:CardContentControl.ContentTemplate>
        <DataTemplate>
            <Grid Padding="16">
                <!-- ... -->
            </Grid>
        </DataTemplate>
    </utu:CardContentControl.ContentTemplate>
</utu:CardContentControl>
```

Rules of thumb:

- **Full-width row card** (e.g. summary card across a page): `HorizontalAlignment="Stretch"`.
- **Column- or pane-filling card** (e.g. a card that should fill a `*` row in a `Grid`): `HorizontalAlignment="Stretch"` **and** `VerticalAlignment="Stretch"`.
- **Card inside an `ItemsRepeater`/`ListView` item template that should fill the item slot**: `HorizontalAlignment="Stretch"`.
- **Compact / chip-style card sized to its content**: leave alignment unset (default behavior).

Setting `Width`/`Height` on the inner content of the `ContentTemplate` will not stretch the card — the alignment must be set on the `CardContentControl` element.

## Style Variants

`CardContentControl` exposes three semantic style keys. Each design-theme provides its own concrete look; the key names are shared across themes.

| Style | When to use |
|-------|-------------|
| `ElevatedCardContentControlStyle` | Subtle z-axis elevation; default choice for surfaced content |
| `FilledCardContentControlStyle` | Background tint, no elevation; use on busy backgrounds |
| `OutlinedCardContentControlStyle` | Stroke instead of fill/elevation; secondary content |

## Related Skills

- [[uno-toolkit-shadowcontainer]] — Custom shadow effects
- [[uno-toolkit-lightweight-styling]] — Resource key overrides for cards
