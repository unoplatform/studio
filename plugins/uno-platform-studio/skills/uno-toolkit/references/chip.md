# Uno Toolkit Chip

## Key Principles (Stable)

- **A pill the user selects or toggles is a `Chip`**, not a rounded `Border` or a restyled `Button`: a category filter, a tag picker, a row of quick options. A read-only label such as a status tag is not selected by the user, and a `Border` stays right for it.
- `Chip` is derived from `ToggleButton` — can be checked/unchecked
- Styles (Material and Simple only; Cupertino ships no Chip styles), picked by what the chip does:
  - `FilterChipStyle` — turns a filter or a choice on and off
  - `AssistChipStyle` — starts an action
  - `InputChipStyle` — holds something the user entered; usually with `CanRemove="True"`
  - `SuggestionChipStyle` — offers a suggested reply or value
- **A bound, selectable row of chips is an `ItemsRepeater` with `ItemsRepeaterExtensions`**: the `Chip` is the root of the `ItemTemplate`, and the extension sets `IsChecked` for you (see `references/itemsrepeater-extensions.md`). It is the same selection surface as every other repeated list, and the row takes any `ItemsRepeater` layout.
- `ChipGroup` is the `ItemsControl` alternative, for a small fixed set written inline or when you need its item events: `SelectionMode` (None, Single, SingleOrNone, Multiple), `SelectedItem`, `SelectedItems`, `SelectionMemberPath`, and the `ItemClick`, `ItemChecked`, `ItemUnchecked`, `ItemRemoving`, `ItemRemoved` events
- `CanRemove="True"` enables a remove button on the chip
- Icon support via `Icon` property
- `Chip` has no property of its own that makes it display-only. For a chip the user cannot press, set `IsHitTestVisible="False"` and `IsTabStop="False"`; `IsEnabled="False"` draws it disabled.
- XAML namespace: `xmlns:utu="using:Uno.Toolkit.UI"`

## A selectable row of chips

```xml
<!-- WRONG: a pill drawn by hand has no checked, pointer-over or focus state -->
<Border CornerRadius="16" Padding="12,6" Background="{ThemeResource SecondaryContainerBrush}">
    <TextBlock Text="{Binding Name}" />
</Border>

<!-- RIGHT: chips in an ItemsRepeater, selection bound through the extension -->
<ItemsRepeater ItemsSource="{Binding Categories}"
               utu:ItemsRepeaterExtensions.SelectionMode="Single"
               utu:ItemsRepeaterExtensions.SelectedItem="{Binding SelectedCategory, Mode=TwoWay}">
    <ItemsRepeater.Layout>
        <StackLayout Orientation="Horizontal" Spacing="8" />
    </ItemsRepeater.Layout>
    <ItemsRepeater.ItemTemplate>
        <DataTemplate>
            <utu:Chip Content="{Binding Name}" Style="{StaticResource FilterChipStyle}" />
        </DataTemplate>
    </ItemsRepeater.ItemTemplate>
</ItemsRepeater>
```

Use `SelectionMode="Multiple"` with `SelectedItems` for filters that combine. Do not bind `IsChecked` on the `Chip` as well: the extension owns it.

## Docs, for what this page does not cover

Use these when the sections above do not answer the question: search, then fetch only the section you need, with the `anchor` from the search result. A whole page stays in context for the rest of the session.

### Fetch the Chip Documentation

```
uno_platform_docs_search("Uno Toolkit Chip ChipGroup filter assist input suggestion")
```

Primary documentation page:
- **Chip & ChipGroup**: `external/uno.toolkit.ui/doc/controls/ChipAndChipGroup.md`

Fetch the page:

```
uno_platform_docs_fetch(sourcePath="external/uno.toolkit.ui/doc/controls/ChipAndChipGroup.md", anchor="…")
```

### For How-To / Walkthrough

```
uno_platform_docs_search("Chip ChipGroup howto walkthrough selection filter")
```

## Related Skills

- `references/itemsrepeater-extensions.md` — Selection for a row of chips
- `references/segmented-controls.md` — Alternative for mutually exclusive options
