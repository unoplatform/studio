# Shell: Bottom TabBar — Reference Templates

Ready-to-use templates for a bottom TabBar navigation shell. Copy, substitute placeholders, and use.

## Icon Lookup Table

| Page Concept | Glyph | Unicode |
|---|---|---|
| Home / Dashboard | `&#xE80F;` | `\uE80F` |
| Search / Explore | `&#xE721;` | `\uE721` |
| Settings / Gear | `&#xE713;` | `\uE713` |
| Profile / Person | `&#xE77B;` | `\uE77B` |
| Favorites / Heart | `&#xE734;` | `\uE734` |
| Add / Plus | `&#xE710;` | `\uE710` |
| List / Bullets | `&#xE8FD;` | `\uE8FD` |
| Chart / Analytics | `&#xE9D9;` | `\uE9D9` |
| Calendar / Date | `&#xE787;` | `\uE787` |
| Mail / Messages | `&#xE715;` | `\uE715` |
| Cart / Shopping | `&#xE7BF;` | `\uE7BF` |
| Notification / Bell | `&#xEA8F;` | `\uEA8F` |

## Required XAML Namespaces

```xml
xmlns:uen="using:Uno.Extensions.Navigation.UI"
xmlns:utu="using:Uno.Toolkit.UI"
```

## SHELL_PAGE_NAME.xaml (Shell)

The content area Grid with `Region.Navigator="Visibility"` must be **empty** — the navigation framework injects views at runtime from registered routes.

```xml
<Page x:Class="SHELL_PAGE_CLASS"
      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      xmlns:uen="using:Uno.Extensions.Navigation.UI"
      xmlns:utu="using:Uno.Toolkit.UI"
      Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">

    <Grid>
        <Grid uen:Region.Attached="True">
            <Grid.RowDefinitions>
                <RowDefinition />
                <RowDefinition Height="Auto" />
            </Grid.RowDefinitions>

            <!-- Content area: visibility-based region switching (empty — framework injects views) -->
            <Grid Grid.Row="0"
                  uen:Region.Attached="True"
                  uen:Region.Navigator="Visibility" />

            <!-- Bottom TabBar -->
            <utu:TabBar Grid.Row="1"
                        uen:Region.Attached="True"
                        Style="{StaticResource BottomTabBarStyle}">
                <!-- REPLACE: One TabBarItem per Tab page -->
                <utu:TabBarItem uen:Region.Name="PAGE1_NAME"
                                Content="PAGE1_LABEL"
                                Style="{StaticResource MaterialBottomTabBarItemStyle}">
                    <utu:TabBarItem.Icon>
                        <FontIcon Glyph="PAGE1_ICON_GLYPH" />
                    </utu:TabBarItem.Icon>
                </utu:TabBarItem>
                <utu:TabBarItem uen:Region.Name="PAGE2_NAME"
                                Content="PAGE2_LABEL"
                                Style="{StaticResource MaterialBottomTabBarItemStyle}">
                    <utu:TabBarItem.Icon>
                        <FontIcon Glyph="PAGE2_ICON_GLYPH" />
                    </utu:TabBarItem.Icon>
                </utu:TabBarItem>
                <utu:TabBarItem uen:Region.Name="PAGE3_NAME"
                                Content="PAGE3_LABEL"
                                Style="{StaticResource MaterialBottomTabBarItemStyle}">
                    <utu:TabBarItem.Icon>
                        <FontIcon Glyph="PAGE3_ICON_GLYPH" />
                    </utu:TabBarItem.Icon>
                </utu:TabBarItem>
            </utu:TabBar>
        </Grid>
    </Grid>
</Page>
```

## Page Template (PageName.xaml)

Create one `.xaml` + `.xaml.cs` pair per page in the appropriate project folder.

```xml
<Page x:Class="PAGE_CLASS"
      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">

    <Grid>
        <TextBlock Text="PAGE_NAME"
                   FontSize="24"
                   HorizontalAlignment="Center"
                   VerticalAlignment="Center" />
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

```csharp
private static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
{
    views.Register(
        new ViewMap(ViewModel: typeof(ShellModel)),
        new ViewMap<SHELL_PAGE_NAME>(),
        // REPLACE: One ViewMap per page
        new ViewMap<PAGE1_NAMEPage>(),
        new ViewMap<PAGE2_NAMEPage>(),
        new ViewMap<PAGE3_NAMEPage>()
    );

    routes.Register(
        new RouteMap("", View: views.FindByViewModel<ShellModel>(),
            Nested:
            [
                new RouteMap("Main", View: views.FindByView<SHELL_PAGE_NAME>(),
                    IsDefault: true,
                    Nested:
                    [
                        // REPLACE: One RouteMap per Tab page
                        new RouteMap("PAGE1_NAME", View: views.FindByView<PAGE1_NAMEPage>(), IsDefault: true),
                        new RouteMap("PAGE2_NAME", View: views.FindByView<PAGE2_NAMEPage>()),
                        new RouteMap("PAGE3_NAME", View: views.FindByView<PAGE3_NAMEPage>())
                    ]
                )
            ]
        )
    );
}
```

## Substitution Rules

1. Replace `SHELL_PAGE_CLASS` with the full `x:Class` value for the shell page (e.g., `MyApp.Presentation.MainPage`)
2. Replace `SHELL_PAGE_NAME` with the class name of the shell page (e.g., `MainPage`). This is the page that hosts the TabBar navigation chrome.
3. Replace `PAGE_CLASS` with the full `x:Class` value for each content page (e.g., `MyApp.Presentation.HomePage`)
4. Replace `PAGE_NAMESPACE` with the namespace for content pages (e.g., `MyApp.Presentation`)
5. Replace `PAGE1_NAME`, `PAGE2_NAME`, `PAGE3_NAME` with route names from the plan (e.g., `Home`, `Search`, `Profile`)
6. Replace `PAGE1_LABEL`, `PAGE2_LABEL`, `PAGE3_LABEL` with display labels (or use `x:Uid` for localization)
7. Replace `PAGE1_ICON_GLYPH`, etc. with glyphs from the Icon Lookup Table above (e.g., `&#xE80F;`)
8. Add or remove `TabBarItem` entries to match the number of Tab pages in the plan
9. The `BottomTabBarStyle` goes on the `TabBar` container; `MaterialBottomTabBarItemStyle` goes on each `TabBarItem`

## Route Registration Rules

1. Route names (first argument to `RouteMap`) MUST match the `uen:Region.Name` values in `SHELL_PAGE_NAME.xaml`
2. Tab page routes are nested under the `"Main"` route so only the content area updates, not the entire page
3. The empty string `""` route is the root shell route
4. `ViewMap` entries without a ViewModel are valid

## Localization Template (Strings/en/Resources.resw)

```xml
<?xml version="1.0" encoding="utf-8"?>
<root>
  <data name="PAGE1_NAME_Tab.Content" xml:space="preserve">
    <value>PAGE1_LABEL</value>
  </data>
  <data name="PAGE1_NAME_Tab.[using:Microsoft.UI.Xaml.Automation]AutomationProperties.Name" xml:space="preserve">
    <value>PAGE1_LABEL tab</value>
  </data>
  <!-- Repeat for each tab -->
</root>
```

When using `x:Uid`, set `x:Uid="PAGE1_NAME_Tab"` on the `TabBarItem` instead of `Content="PAGE1_LABEL"`.
