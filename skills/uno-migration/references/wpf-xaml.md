# WPF XAML and Controls

Replacements for WPF XAML features WinUI does not have, and the control mapping. Uno Platform uses the WinUI 3 XAML dialect on every target.

## Unsupported features

### `x:Static`

WinUI has no `x:Static`. Pick the replacement by what the static member holds:

```xml
<!-- WPF -->
<TextBlock Text="{x:Static local:Constants.AppTitle}" />

<!-- A constant string or number: a resource -->
<x:String x:Key="AppTitle">My Application</x:String>
<TextBlock Text="{StaticResource AppTitle}" />

<!-- A static property or field: x:Bind reaches static members -->
<TextBlock Text="{x:Bind local:Constants.AppTitle}" />
```

A custom `MarkupExtension` subclass is also supported when a static lookup is used in many places.

### `StringFormat` and `MultiBinding`

WinUI bindings have neither. Use an `x:Bind` function binding, `Run` elements, a computed property, or a converter:

```xml
<!-- WPF -->
<TextBlock Text="{Binding Total, StringFormat={}{0:C}}" />
<TextBlock>
    <TextBlock.Text>
        <MultiBinding StringFormat="{}{0} {1}">
            <Binding Path="FirstName" />
            <Binding Path="LastName" />
        </MultiBinding>
    </TextBlock.Text>
</TextBlock>

<!-- Function binding -->
<TextBlock Text="{x:Bind ViewModel.Total.ToString('C'), Mode=OneWay}" />

<!-- Runs -->
<TextBlock>
    <Run Text="{Binding FirstName}" />
    <Run Text=" " />
    <Run Text="{Binding LastName}" />
</TextBlock>

<!-- Computed property: public string FullName => $"{FirstName} {LastName}"; -->
<TextBlock Text="{Binding FullName}" />
```

Search resource dictionaries and control templates for `MultiBinding` and `StringFormat` too, not only pages.

### Triggers

`Style.Triggers`, `DataTrigger`, `Trigger`, and `MultiTrigger` do not exist. Use `VisualStateManager` with `StateTrigger`:

```xml
<!-- WPF -->
<Style.Triggers>
    <DataTrigger Binding="{Binding IsActive}" Value="True">
        <Setter Property="Background" Value="Green" />
    </DataTrigger>
</Style.Triggers>

<!-- WinUI -->
<VisualStateManager.VisualStateGroups>
    <VisualStateGroup>
        <VisualState x:Name="Active">
            <VisualState.StateTriggers>
                <StateTrigger IsActive="{x:Bind ViewModel.IsActive, Mode=OneWay}" />
            </VisualState.StateTriggers>
            <VisualState.Setters>
                <Setter Target="RootBorder.Background" Value="{ThemeResource PrimaryContainerBrush}" />
            </VisualState.Setters>
        </VisualState>
    </VisualStateGroup>
</VisualStateManager.VisualStateGroups>
```

The `VisualStateManager.VisualStateGroups` go on the root element of the page or control template, and setters target named elements. `AdaptiveTrigger` covers window-size triggers. A trigger that only swaps a value (a color for a boolean) is often simpler as a converter or a bound property.

### Event triggers and behaviors

`Interaction.Triggers` with `InvokeCommandAction` becomes a `Command` binding where the control has one:

```xml
<Button Content="Save" Command="{Binding SaveCommand}" />
```

For events without a `Command` property, use `utu:CommandExtensions.Command` from the `uno-toolkit` skill (`references/command-extensions.md`).

### `RelativeSource AncestorType`

WinUI has no `FindAncestor`. Use `ElementName`, `x:Bind` to the page, or `AncestorBinding` from the `uno-toolkit` skill (`references/ancestor-binding.md`).

### Platform-specific XAML

Uno Platform XAML has platform prefixes (`win`, `not_win`, `android`, `ios`, `wasm`, `skia`, and more) that include an element or a single property on some targets only. Prefixes excluded on Windows (`not_win`, `android`, `wasm`) get an arbitrary namespace and go in `mc:Ignorable`; prefixes included on Windows (`win`, `not_android`) use the presentation namespace and stay out of it. See `platform-specific-xaml.md` in the docs.

```xml
<Page xmlns:win="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:not_win="http://uno.ui/not_win"
      xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
      mc:Ignorable="not_win">
    <win:Button Content="Open in Explorer" />
    <not_win:TextBlock Text="Available on Windows only" />
</Page>
```

`win` means the WinAppSDK head. The Skia desktop head is `not_win` even when it runs on Windows, so a feature that needs Windows at run time on the desktop head is shown or hidden from a bound property set by an `OperatingSystem.IsWindows()` check instead.

## Controls

| WPF | WinUI / Uno Platform | Notes |
|---|---|---|
| `DataGrid` | Community Toolkit 7.x `DataGrid` | `Uno.CommunityToolkit.WinUI.UI.Controls.DataGrid` on Uno targets, `CommunityToolkit.WinUI.UI.Controls.DataGrid` on WinAppSDK; fetch `uno-community-toolkit-v7.md`. For simple read-only tables, a `ListView` with a column `Grid` in its item template is lighter. Commercial grids are another option |
| `Menu`, `MenuItem` | `MenuBar`, `MenuBarItem`, `MenuFlyoutItem` | Add `KeyboardAccelerator`s; WPF `InputGestureText` was display only |
| `ContextMenu` | `ContextFlyout` with `MenuFlyout` | |
| `ToolBar` | `CommandBar` with `AppBarButton` | |
| `StatusBar` | A `Grid` row at the bottom of the page | |
| `TabControl` | `TabView` (document tabs), Toolkit `TabBar` (navigation) | |
| `DockPanel` | `Grid` rows and columns | |
| `WrapPanel` | `ItemsRepeater` with a wrapping layout, or Toolkit `AutoLayout` | |
| `ListBox` | `ListView` | |
| `Expander` | `Expander` | Built in to WinUI |
| `DatePicker` | `CalendarDatePicker` (text box with a calendar) or `DatePicker` (spinner) | |
| `WebBrowser` | `WebView2` | |
| `RichTextBox`, `FlowDocument` | `RichEditBox` (limited) or `RichTextBlock` (read-only) | Check the features you need on each target |
| `Viewbox` | `Viewbox` | |
| Custom zoom and pan borders | Toolkit `ZoomContentControl` | `ZoomLevel`, `MinZoomLevel`, `MaxZoomLevel`, `AutoFitToCanvas`, `IsPanAllowed` |
| `Effect` (drop shadow, blur) | `ThemeShadow` with `Translation`, or Toolkit `ShadowContainer` | `BitmapEffect` pixel shaders have no equivalent |
| WPF-UI `FluentWindow`, MahApps `MetroWindow` | The app's `Window` | Title bar customization goes through `AppWindow` on desktop |
| WPF-UI `SymbolIcon`, MahApps icon packs | `FontIcon`, `SymbolIcon`, `PathIcon` | |
| `WindowsFormsHost` | None | Rewrite the hosted UI |

## Resources and theme

WPF theme libraries (MahApps, MaterialDesignInXaml, WPF-UI) do not carry over. Keep the new app's theme (Simple from the recommended template, or Material when the WPF app used a Material library) and restyle through the semantic keys in the `uno-themes` skill: brushes such as `OnSurfaceBrush`, typography such as `BodyMedium`, control styles such as `FilledButtonStyle`.

WinUI Fluent keys (`BodyTextBlockStyle`, `ApplicationPageBackgroundThemeBrush`) are not part of Uno Material or Simple. A key that no resource dictionary defines throws `XamlParseException` on the WinAppSDK head; on the other targets it logs "Couldn't statically resolve resource" and leaves the property unset, which reads as missing text color or background. When styling looks wrong on one target only, check the resource keys first.

Custom `ResourceDictionary` files port as they are, minus the unsupported features above. `DynamicResource` becomes `ThemeResource` for theme-dependent values.

## Common mistakes

- Emulating triggers with code-behind property setters instead of `VisualStateManager`.
- Porting `MultiBinding` converters (`IMultiValueConverter`) instead of computing the value in the view model.
- Mixing Fluent resource keys into a Material or Simple app.
- Checking pages for unsupported features but not styles, templates, and resource dictionaries.
