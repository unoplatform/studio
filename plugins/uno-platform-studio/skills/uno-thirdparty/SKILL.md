---
name: uno-thirdparty
description: "Third-party controls for Uno Platform apps: charts (LiveCharts2 CartesianChart/PieChart, ScottPlot WinUIPlot), maps (Mapsui MapControl with OpenStreetMap tiles, pins, tap info) and data grids (WinUI.TableView, the Windows Community Toolkit DataGrid). Covers which package to pick, which platforms and Uno.Sdk versions each supports, installation with central package management, binding them to MVUX feeds, and matching them to the Uno Themes palette and dark mode. Use whenever an Uno app needs a chart, graph, plot, dashboard, KPI trend, sparkline, map, location picker, store locator, route or pin view, data grid, table, spreadsheet-like list with columns, sorting or filtering, even when the user only says 'show the sales by month' or 'list the orders in a table'. Read it before adding any NuGet package for these, and before drawing one by hand with Canvas, Path or Grid."
metadata:
  author: uno-platform
  category: thirdparty
---

# Uno third-party controls

Uno Platform ships no chart, map or data grid of its own. Business apps nearly always need one, and agents without guidance guess a package that does not support Uno, skip a platform, or draw the control by hand with `Canvas` and `Path`. This skill names the packages that work, how to install them, and the binding and theming traps each one has.

## Workflow

1. Read `global.json` for the `Uno.Sdk` version, then `references/packages.md`. Packages built for Uno 6 do not run on Uno.Sdk 7.0 previews; say so to the user rather than forcing them in.
2. Read the reference for the control: `references/charts.md`, `references/maps.md` or `references/data-grids.md`.
3. Charts and maps draw with SkiaSharp and ignore `{ThemeResource}`. Wire their colors with `references/skia-theme-colors.md`.
4. Build once and look at the running app (the `uno-testing` skill): an empty chart or a tile-less map compiles cleanly.

## Topic map

| Task / need | Read | Package and key API |
|-------------|------|---------------------|
| Which package, platforms, Uno 6 vs 7, install, Windows head | `references/packages.md` | `Directory.Packages.props`, `PackageReference` |
| Bar, line, area, pie chart bound to MVUX data | `references/charts.md` | `LiveChartsCore.SkiaSharpView.Uno.WinUI`: `lvc:CartesianChart`, `Series`, `XAxes` |
| Scientific or signal plot, thousands of points | `references/charts.md` | `ScottPlot.WinUI`: `scottplot:WinUIPlot`, `Plot.Add.Signal`, `Refresh()` |
| Map with tiles, pins, tap on a pin, center and zoom | `references/maps.md` | `Mapsui.Uno.WinUI`: `mapsui:MapControl`, `OpenStreetMap.CreateTileLayer`, `MemoryLayer` |
| Table with columns, sorting, filtering, read-only list | `references/data-grids.md` | `WinUI.TableView`: `tv:TableView`, `TableViewTextColumn` |
| The Windows Community Toolkit `DataGrid` | `references/data-grids.md` | `Uno.CommunityToolkit.WinUI.UI.Controls.DataGrid`: `wct:DataGrid` |
| Chart or map colors from the theme, dark mode | `references/skia-theme-colors.md` | `ThemeDictionaries`, `ActualThemeChanged` |

## Critical rules

- **Use a package from this skill, never a hand-drawn approximation.** Bars made of `Rectangle`s, a line of `Path` segments, or a `Grid` of `TextBlock`s pretending to be a table lose axes, scaling, sorting, accessibility and virtualization.
- **Check the Uno.Sdk major first.** On Uno.Sdk 6.x every package here builds and runs. On 7.0 previews the SkiaSharp-based ones (LiveCharts2, ScottPlot, Mapsui) fail with `UNOB0023` and WinUI.TableView 1.5.1 fails with `CS0012` on the `Uno` assembly, until their authors publish 7.0 builds. Never set `UnoDisableUNOB0023Validation` to get past it: the app then runs against reference assemblies and fails at startup.
- **Never bind a chart's `Series` to an `IFeed<ISeries[]>` or `IState<ISeries[]>`.** The chart stays empty, with no error. Wrap the series in a record (`IFeed<SalesChart>`) and bind `{Binding Chart.Series}`.
- **Give OpenStreetMap a user agent that names the app** (`OpenStreetMap.CreateTileLayer("FieldSites/1.0")`). A placeholder such as `example.com` gets every tile replaced by a 403 "Access blocked" image.
- **Write `Mapsui.UI.WinUI.MapControl` and `Mapsui.Styles.Brush` in full in C#.** Both names collide with WinUI types (`Microsoft.UI.Xaml.Controls.MapControl`, `Microsoft.UI.Xaml.Media.Brush`); in XAML the `mapsui:` prefix is enough.
- **Data grids bound to MVUX feeds are read-only** (`IsReadOnly="True"`). MVUX items are immutable records: edit through a command that updates an `IListState<T>` (the `uno-mvux` skill, `references/liststate.md`).
- **Set `AutoGenerateColumns="False"` and declare the columns** for anything a user sees. Generated columns use property names as headers and every property, including ids.
- **Chart and map colors never come from `Application.Current.Resources[key]`.** That lookup keeps answering for the startup theme after a switch to dark mode. Use the helper in `references/skia-theme-colors.md`.

## Related skills

- `uno-mvux` for the feeds and states the controls bind to (`references/listfeed.md`, `references/liststate.md`).
- `uno-themes` for the color keys (`PrimaryColor`, `OnSurfaceColor`, `SurfaceColor`) the charts are painted with.
- `uno-testing` to confirm the control rendered data: give each one an `AutomationProperties.AutomationId`.
