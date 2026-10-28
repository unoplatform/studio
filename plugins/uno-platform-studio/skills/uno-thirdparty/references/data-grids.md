# Data grids: WinUI.TableView and the Community Toolkit DataGrid

Use **WinUI.TableView** by default: one package for every head, sorting, filtering, column resizing and export built in, and maintained. Use the **Windows Community Toolkit 7 `DataGrid`** when the app already uses it or the user names it; it needs a second package on the Windows head (`packages.md`, in this skill's `references/`).

Both bind an MVUX `IListFeed<T>` as `ItemsSource` directly, follow light and dark mode without extra work, and are a `ListView`/`Control` you can give an `AutomationProperties.AutomationId`.

## WinUI.TableView

```xml
<Page xmlns:tv="using:WinUI.TableView" ...>
  <tv:TableView x:Name="SalesTable"
                AutomationProperties.AutomationId="SalesTable"
                ItemsSource="{Binding Sales}"
                AutoGenerateColumns="False"
                IsReadOnly="True"
                SelectionMode="Single"
                SelectionUnit="Row">
    <tv:TableView.Columns>
      <tv:TableViewTextColumn Header="Region" Binding="{Binding Region}" />
      <tv:TableViewNumberColumn Header="Units" Binding="{Binding Units}" />
      <tv:TableViewDateColumn Header="Shipped" Binding="{Binding ShippedOn}" />
    </tv:TableView.Columns>
  </tv:TableView>
</Page>
```

- Column types: `TableViewTextColumn`, `TableViewNumberColumn`, `TableViewCheckBoxColumn`, `TableViewToggleSwitchColumn`, `TableViewComboBoxColumn`, `TableViewDateColumn`, `TableViewTimeColumn`, `TableViewHyperlinkColumn`, `TableViewTemplateColumn` (`CellTemplate`, `EditingTemplate`). Bound columns take `Binding`; every column takes `Header`, `Width`, `MinWidth`, `CanSort`, `CanFilter`, `IsReadOnly`.
- Table switches: `CanSortColumns`, `CanFilterColumns`, `CanResizeColumns`, `CanReorderColumns`, `FrozenColumnCount`, `ShowExportOptions`, `GridLinesVisibility`, `AlternateRowBackground`, `RowDetailsTemplate`, `HeadersVisibility`.
- `SelectionUnit` is `CellOrRow` by default: a click selects a cell. Set `Row` for row selection.
- `TableView` derives from `ListView`, but its cells take the pointer: `IsItemClickEnabled` with `ItemClick` or `utu:CommandExtensions.Command` never fires on a cell click.

## Community Toolkit DataGrid

```xml
<Page xmlns:wct="using:CommunityToolkit.WinUI.UI.Controls" ...>
  <wct:DataGrid x:Name="SalesGrid"
                AutomationProperties.AutomationId="SalesGrid"
                ItemsSource="{Binding Sales}"
                AutoGenerateColumns="False"
                IsReadOnly="True">
    <wct:DataGrid.Columns>
      <wct:DataGridTextColumn Header="Region" Binding="{Binding Region}" />
      <wct:DataGridTextColumn Header="Units" Binding="{Binding Units}" />
    </wct:DataGrid.Columns>
  </wct:DataGrid>
</Page>
```

Other columns: `DataGridCheckBoxColumn`, `DataGridComboBoxColumn`, `DataGridTemplateColumn`. Sorting needs `CanUserSortColumns="True"` and a `Sorting` handler that reorders the source; the DataGrid does not sort for you.

## Selection with MVUX

Neither grid drives MVUX selection. `.Selection(state)` on the list feed, and a `TwoWay` `SelectedItem` binding to an `IState<T>`, both leave the state empty while the row shows as selected. Forward the grid's `SelectionChanged` to the Model's command from code-behind:

```csharp
// Model
public async ValueTask Open(Sale sale, CancellationToken ct) => await SelectedSale.Set(sale, ct);

// Page constructor
SalesTable.SelectionChanged += (_, _) =>
{
    if (DataContext is SalesViewModel vm && SalesTable.SelectedItem is Sale sale)
    {
        vm.Open.Execute(sale);
    }
};
```

The same handler works on the DataGrid. A plain `ListView` does honor `.Selection(state)`: use one when the user asked for a list, not a table.

## Rules

- **`AutoGenerateColumns="False"` with declared columns** for anything a user sees: generated columns show every property, ids included, with the property name as the header.
- **`IsReadOnly="True"` on MVUX data.** The rows are immutable records. Edit through a command that updates an `IListState<T>` (the `uno-mvux` skill, `references/liststate.md`), then the grid shows the new record.
- **A grid needs a bounded height**: a `*` row or an explicit `Height`. Inside a `StackPanel` or `ScrollViewer` it measures infinite, loses virtualization, and renders every row.
- Formatting: WinUI bindings have no `StringFormat`. Bind to a formatted property on the record (`RevenueText`) or use a template column; only `TableViewDateColumn` takes a pattern (`DateFormat`).

## Sources

- WinUI.TableView: w-ahmad.github.io/WinUI.TableView and its sample app.
- DataGrid: `uno_platform_docs_search("Windows Community Toolkit 7 DataGrid Uno")`, page `uno-community-toolkit-v7.md`, and the Uno.Samples `UnoWCTDataGridSample`.
