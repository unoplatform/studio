---
name: uno-previews
description: "Author preview files — a page, UserControl, style or data template rendered on its own, with the data it needs."
when_to_use: "Use when asked to add previews for pages or controls, to show a page with sample or mock data at design time, to showcase a named Style or DataTemplate, to show several states of one control side by side, or to decide between a standalone preview file and a PreviewGroup. Previews are plain source files: author them directly — nothing needs to be running. For a page bound to an MVUX view-model, pair with `uno-mvux-mocking` to supply the data."
metadata:
  author: uno-platform
  version: "1.0"
  category: previews
---

# Previews — Agent Skill

A preview renders one page, `UserControl`, control, style or data template on its own in Hot Design's **Previews** panel, with the data you give it. Each hand-authored preview is two files — a XAML file whose root is `<hd:Preview>`, and its code-behind — and it is found by reflection when the app runs. There is nothing to register and no package to add: the Uno SDK adds the reference in Debug builds.

## Before You Write One

- **Every public control with a parameterless constructor already has a preview**, generated implicitly — a page shows up with no file at all. Only author a preview to add what the implicit one lacks: **data**, a **style**, a **data template**, or a **particular state**.
- **A page that normally gets its view-model from navigation has none in a preview.** Supply it, or its bindings render empty.

## File Location

Previews go in the folder named by the `HotDesignPreviewsFolder` MSBuild property. **The default is `HotDesignPreviews/`**, at the project root:

```text
<ProjectDirectory>/HotDesignPreviews/WeatherPagePreview.xaml
<ProjectDirectory>/HotDesignPreviews/WeatherPagePreview.xaml.cs
```

Check the `.csproj` and `Directory.Build.props` for an override before placing files. The Uno SDK excludes this folder from Release builds, so previews and any mock data they use never ship.

Any namespace works, since discovery is by reflection; the `x:Class` in the XAML must match the class in the code-behind.

## Choosing a Variant

| Need | Variant |
| --- | --- |
| The control loads its own data, you want a named entry | Type only |
| Showcase a named `Style` | Style (`styleKey`) |
| The control binds to a simple value | DataContext — value |
| The page binds to a view-model | DataContext — view-model |
| Showcase a named `DataTemplate` | DataTemplate (`dataTemplateKey`) |
| Several states of one control, kept together | `PreviewGroup` |

## Type Only

```xml
<hd:Preview x:Class="MyApp.WeatherPagePreview"
            xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
            xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
            xmlns:local="using:MyApp"
            xmlns:hd="using:Uno.UI.HotDesign">

    <local:WeatherPage />
</hd:Preview>
```

```csharp
using Uno.UI.HotDesign;

namespace MyApp;

[Preview("Weather Page", typeof(WeatherPage))]
public sealed partial class WeatherPagePreview : Preview
{
    public WeatherPagePreview() => this.InitializeComponent();
}
```

`[Preview(name, controlType, styleKey = null, dataTemplateKey = null)]` names the entry and files it under its control type.

## DataContext — View-Model

Override `LoadDataContext()`. Its result becomes the `DataContext` of the preview's content, so the page's bindings resolve against it. The XAML is the same as Type Only.

For an **MVUX** page, return a mocked view-model — see `uno-mvux-mocking`:

```csharp
using Uno.HotTesting.Reactive;
using Uno.UI.HotDesign;

namespace MyApp;

[Preview("Weather Page — Sunny", typeof(WeatherPage))]
public sealed partial class WeatherPagePreview : Preview
{
    public WeatherPagePreview() => this.InitializeComponent();

    protected override object? LoadDataContext()
        => WeatherViewModelMock.Create(new WeatherModelMock
        {
            Current = FeedMock.Value(new WeatherInfo("Montreal", 21, "Sunny")),
            Forecast = ListFeedMock.Value(new WeatherInfo("Monday", 19, "Rain")),
        });
}
```

For any other page, return a populated view-model instance directly. When several previews share sample data, keep it in one static class in the previews folder.

## DataContext — Value

For a control that binds to a simple value with `{Binding}`, return the value:

```csharp
[Preview("Default", typeof(Button))]
public sealed partial class GreetingButtonPreview : Preview
{
    public GreetingButtonPreview() => this.InitializeComponent();

    protected override object? LoadDataContext() => "Click me";
}
```

with `<Button Content="{Binding}" />` as the XAML content.

## Style

Set the style on the element and pass the same key to the attribute:

```xml
<Button Style="{StaticResource AccentButtonStyle}" Content="Accent" />
```

```csharp
[Preview("Accent", typeof(Button), styleKey: "AccentButtonStyle")]
```

## DataTemplate

Put the template on a `ContentControl`; `LoadDataContext()` then supplies the data the template renders. With `dataTemplateKey` set, the preview is filed under **Data Templates** and `controlType` is ignored — pass any representative type.

```xml
<ContentControl ContentTemplate="{StaticResource ForecastRowTemplate}" />
```

```csharp
[Preview("Forecast Row", typeof(ContentControl), dataTemplateKey: "ForecastRowTemplate")]
public sealed partial class ForecastRowPreview : Preview
{
    public ForecastRowPreview() => this.InitializeComponent();

    protected override object? LoadDataContext() => new WeatherInfo("Monday", 19, "Rain");
}
```

Set only one of `styleKey` and `dataTemplateKey`; if both are given, `dataTemplateKey` wins.

## PreviewGroup — Several States in One File

A group holds several named previews of one control. The root is `<hd:PreviewGroup>`, the code-behind derives from `PreviewGroup` with **no** `[Preview]` attribute, and each child is an `<hd:Preview>` with a `PreviewName` and exactly one content element.

A child has no code-behind, so `LoadDataContext()` is not available. Give each child its data with `x:Bind` to a static member:

```xml
<hd:PreviewGroup x:Class="MyApp.WeatherPageStates"
                 xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                 xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                 xmlns:local="using:MyApp"
                 xmlns:hd="using:Uno.UI.HotDesign">

    <hd:Preview PreviewName="Sunny">
        <local:WeatherPage DataContext="{x:Bind local:WeatherPreviewData.Sunny}" />
    </hd:Preview>

    <hd:Preview PreviewName="Loading">
        <local:WeatherPage DataContext="{x:Bind local:WeatherPreviewData.Loading}" />
    </hd:Preview>

</hd:PreviewGroup>
```

```csharp
using Uno.UI.HotDesign;

namespace MyApp;

public sealed partial class WeatherPageStates : PreviewGroup
{
    public WeatherPageStates() => this.InitializeComponent();
}

public static class WeatherPreviewData
{
    public static WeatherViewModel Sunny => WeatherViewModelMock.Create(/* ... */);
    public static WeatherViewModel Loading => WeatherViewModelMock.Create(WeatherModelMock.Empty with
    {
        Current = FeedMock.Loading<WeatherInfo>(),
    });
}
```

Keep `PreviewName` short and state-descriptive (`"Loading"`, not `"Weather page while loading"`): the tree shows it next to the control name. Use one standalone file per preview instead when each needs its own `styleKey` or `dataTemplateKey`.

## Critical Rules

- **The folder is `HotDesignPreviews/` unless the project overrides `HotDesignPreviewsFolder`.** Some pages still say `Previews/`; that is not the SDK default.
- **Don't edit `DefaultPreviews.xaml`.** The designer writes and rewrites that file for its own **Add Preview** button. Hand-authored previews go in their own files.
- **The code-behind must be `partial` and call `InitializeComponent()`**, and its class must match `x:Class`.
- **Build in Debug to check a preview.** Release builds exclude the previews folder, so an error in a preview never shows there.
- **A preview that renders blank usually means its data was never created.** If `LoadDataContext()`, or the static member a group child binds to, throws, the preview shows the page with no `DataContext` and nothing reports the error. For a mocked MVUX page, check the model against the method-group rule in `uno-mvux-mocking`; a single blank area is usually a derived member left out of the mock. An *empty* state is different: a `FeedView` with no `NoneTemplate` renders nothing for it, by design.

## Checking a Preview in a Running App (Optional)

Authoring needs nothing running. To see the result, run the app, open Hot Design, and pick **Previews** in its left bar — the preview sits under its control type, or under **Data Templates**.

An agent with the Uno app MCP can check the same way through `uno_execute_tool`, once the app is running:

1. Call `app_hotdesign_set_mode` with `in_app`. The designer takes a moment to start and answers "not ready" until it has — retry every few seconds.
2. Call `app_hotdesign_set_app_mode` with `previews`, then `app_read_resource` on `hotdesign://previews` for the catalogue.
3. To look at a preview, open it with `app_hotdesign_select_preview` and take a window screenshot. The first time the designer opens in an app, an introduction dialog covers the canvas — make sure it is dismissed before judging what the preview shows. Don't rely on `app_hotdesign_screenshot_preview` for this: it renders off-screen without the `LoadDataContext()` data, so a mocked page looks empty there even when the preview is correct.

## Related Skills

- [[uno-mvux-mocking]] — Generate the mocked view-models previews bind to
- [[uno-mvux-feedview]] — How a FeedView renders each mocked state
- [[uno-themes-material]] — Named styles worth a style preview
