# Shell: Simple Frame — Reference Templates

Ready-to-use templates for a simple Frame-based navigation shell. Copy, substitute placeholders, and use.

## SHELL_PAGE_NAME.xaml (Shell)

```xml
<Page x:Class="SHELL_PAGE_CLASS"
      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      xmlns:uen="using:Uno.Extensions.Navigation.UI"
      Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">

    <Grid>
        <!-- REPLACE: Build your main page content here -->
        <!-- Use uen:Navigation.Request on buttons to navigate to other pages -->
        <StackPanel HorizontalAlignment="Center"
                    VerticalAlignment="Center"
                    Spacing="16">
            <TextBlock Text="APP_TITLE"
                       FontSize="32"
                       HorizontalAlignment="Center" />
            <!-- REPLACE: One button per navigable page -->
            <Button Content="Go to PAGE2_LABEL"
                    HorizontalAlignment="Center"
                    uen:Navigation.Request="PAGE2_NAME" />
        </StackPanel>
    </Grid>
</Page>
```

## Navigation.Request Syntax

- **Navigate forward**: `uen:Navigation.Request="PageName"` — pushes page onto the frame stack
- **Navigate and clear back stack**: `uen:Navigation.Request="-/PageName"` — replaces the current page
- **Navigate back**: `uen:Navigation.Request="!back"` — pops the current page
- **Navigate to nested region**: `uen:Navigation.Request="./RegionName"` — for visibility-based regions

## Page Template (PageName.xaml)

Create one `.xaml` + `.xaml.cs` pair per page in the appropriate project folder.

```xml
<Page x:Class="PAGE_CLASS"
      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      xmlns:uen="using:Uno.Extensions.Navigation.UI"
      Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">

    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition />
        </Grid.RowDefinitions>

        <!-- Top bar with back button -->
        <Button Content="Back"
                uen:Navigation.Request="!back"
                Margin="8" />

        <!-- REPLACE: Page content goes here -->
        <Grid Grid.Row="1">
            <TextBlock Text="PAGE_NAME"
                       FontSize="24"
                       HorizontalAlignment="Center"
                       VerticalAlignment="Center" />
        </Grid>
    </Grid>
</Page>
```

## Page Template (PageName.xaml.cs)

```csharp
namespace PAGE_NAMESPACE;

public sealed partial class PAGE_NAMEPage : Page
{
    public PAGE_NAMEPage()
    {
        this.InitializeComponent();
    }
}
```

## Route Registration (App.xaml.cs)

Add to the existing `RegisterRoutes` method:

```csharp
private static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
{
    views.Register(
        new ViewMap(ViewModel: typeof(ShellModel)),
        new ViewMap<SHELL_PAGE_NAME>(),
        // REPLACE: One ViewMap per page
        new ViewMap<PAGE2_NAMEPage>()
    );

    routes.Register(
        new RouteMap("", View: views.FindByViewModel<ShellModel>(),
            Nested:
            [
                new RouteMap("Main", View: views.FindByView<SHELL_PAGE_NAME>(),
                    IsDefault: true,
                    Nested:
                    [
                        // REPLACE: One RouteMap per secondary page
                        new RouteMap("PAGE2_NAME", View: views.FindByView<PAGE2_NAMEPage>())
                    ]
                )
            ]
        )
    );
}
```

## Substitution Rules

1. Replace `SHELL_PAGE_CLASS` with the full `x:Class` value for the shell page (e.g., `MyApp.Presentation.MainPage`)
2. Replace `SHELL_PAGE_NAME` with the class name of the shell page (e.g., `MainPage`). This is the page that hosts the primary navigation content.
3. Replace `PAGE_CLASS` with the full `x:Class` value for each content page (e.g., `MyApp.Presentation.DetailPage`)
4. Replace `PAGE_NAMESPACE` with the namespace for content pages (e.g., `MyApp.Presentation`)
5. Replace `APP_TITLE` with the app name from the plan
6. Replace `PAGE2_NAME` with the route name of the secondary page (e.g., `Detail`)
7. Replace `PAGE2_LABEL` with the display label (e.g., `"View Details"`)
8. Add more `Button` + `Navigation.Request` pairs for additional pages
9. On secondary pages, always include a back button with `uen:Navigation.Request="!back"`
