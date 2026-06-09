# Shell: Responsive NavigationView + TabBar — Reference Templates

Ready-to-use template for a responsive navigation shell that switches between a bottom TabBar on narrow screens and a sidebar NavigationView on wide screens.

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

## Required XAML Namespaces

```xml
xmlns:uen="using:Uno.Extensions.Navigation.UI"
xmlns:utu="using:Uno.Toolkit.UI"
```

## SHELL_PAGE_NAME.xaml (Responsive Shell)

The content area Grid with `Region.Navigator="Visibility"` must be **empty** — the navigation framework injects views at runtime from registered routes.

```xml
<Page x:Class="SHELL_PAGE_CLASS"
      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      xmlns:uen="using:Uno.Extensions.Navigation.UI"
      xmlns:utu="using:Uno.Toolkit.UI"
      Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">

    <Grid uen:Region.Attached="True">
        <VisualStateManager.VisualStateGroups>
            <VisualStateGroup>
                <!-- Narrow: TabBar visible, NavigationView pane hidden -->
                <VisualState x:Name="Narrow">
                    <VisualState.Setters>
                        <Setter Target="BottomTabs.Visibility" Value="Visible" />
                        <Setter Target="NavView.IsPaneToggleButtonVisible" Value="False" />
                        <Setter Target="NavView.PaneDisplayMode" Value="LeftMinimal" />
                        <Setter Target="NavView.IsPaneOpen" Value="False" />
                    </VisualState.Setters>
                </VisualState>
                <!-- Normal (>=700px): NavigationView visible, TabBar hidden -->
                <VisualState x:Name="Normal">
                    <VisualState.StateTriggers>
                        <AdaptiveTrigger MinWindowWidth="700" />
                    </VisualState.StateTriggers>
                    <VisualState.Setters>
                        <Setter Target="BottomTabs.Visibility" Value="Collapsed" />
                        <Setter Target="NavView.IsPaneToggleButtonVisible" Value="True" />
                        <Setter Target="NavView.IsPaneVisible" Value="True" />
                        <Setter Target="NavView.PaneDisplayMode" Value="Auto" />
                    </VisualState.Setters>
                </VisualState>
                <!-- Wide (>=1000px): NavigationView expanded -->
                <VisualState x:Name="Wide">
                    <VisualState.StateTriggers>
                        <AdaptiveTrigger MinWindowWidth="1000" />
                    </VisualState.StateTriggers>
                    <VisualState.Setters>
                        <Setter Target="BottomTabs.Visibility" Value="Collapsed" />
                        <Setter Target="NavView.IsPaneToggleButtonVisible" Value="True" />
                        <Setter Target="NavView.IsPaneVisible" Value="True" />
                        <Setter Target="NavView.PaneDisplayMode" Value="Auto" />
                    </VisualState.Setters>
                </VisualState>
            </VisualStateGroup>
        </VisualStateManager.VisualStateGroups>

        <!-- NavigationView: visible on Normal/Wide, pane hidden on Narrow -->
        <NavigationView x:Name="NavView"
                        uen:Region.Attached="True"
                        IsSettingsVisible="False"
                        IsBackButtonVisible="Collapsed">
            <NavigationView.MenuItems>
                <!-- REPLACE: One NavigationViewItem per page -->
                <NavigationViewItem uen:Region.Name="PAGE1_NAME"
                                    Content="PAGE1_LABEL">
                    <NavigationViewItem.Icon>
                        <FontIcon Glyph="PAGE1_ICON_GLYPH" />
                    </NavigationViewItem.Icon>
                </NavigationViewItem>
                <NavigationViewItem uen:Region.Name="PAGE2_NAME"
                                    Content="PAGE2_LABEL">
                    <NavigationViewItem.Icon>
                        <FontIcon Glyph="PAGE2_ICON_GLYPH" />
                    </NavigationViewItem.Icon>
                </NavigationViewItem>
                <NavigationViewItem uen:Region.Name="PAGE3_NAME"
                                    Content="PAGE3_LABEL">
                    <NavigationViewItem.Icon>
                        <FontIcon Glyph="PAGE3_ICON_GLYPH" />
                    </NavigationViewItem.Icon>
                </NavigationViewItem>
            </NavigationView.MenuItems>

            <NavigationView.Content>
                <Grid>
                    <Grid.RowDefinitions>
                        <RowDefinition />
                        <RowDefinition Height="Auto" />
                    </Grid.RowDefinitions>

                    <!-- Shared content area: used by both NavigationView and TabBar (empty — framework injects views) -->
                    <Grid Grid.Row="0"
                          uen:Region.Attached="True"
                          uen:Region.Navigator="Visibility" />

                    <!-- Bottom TabBar: visible on Narrow, hidden on Normal/Wide -->
                    <utu:TabBar x:Name="BottomTabs"
                                Grid.Row="1"
                                uen:Region.Attached="True"
                                Style="{StaticResource BottomTabBarStyle}"
                                Visibility="Collapsed">
                        <!-- REPLACE: One TabBarItem per page (must mirror NavigationViewItems) -->
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
            </NavigationView.Content>
        </NavigationView>
    </Grid>
</Page>
```

## Breakpoint Summary

| State | Width | TabBar | NavigationView Pane | Use Case |
|---|---|---|---|---|
| Narrow | < 700px | Visible | Hidden | Mobile phones |
| Normal | 700-999px | Hidden | Visible (auto) | Tablets |
| Wide | >= 1000px | Hidden | Visible (expanded) | Desktops |

## Route Registration (App.xaml.cs)

Same pattern as TabBar and NavigationView shells:

```csharp
private static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
{
    views.Register(
        new ViewMap(ViewModel: typeof(ShellModel)),
        new ViewMap<SHELL_PAGE_NAME>(),
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
2. Replace `SHELL_PAGE_NAME` with the class name of the shell page (e.g., `MainPage`). This is the page that hosts the responsive navigation chrome.
3. Replace `PAGE1_NAME`, `PAGE2_NAME`, `PAGE3_NAME` with route names from the plan
4. Replace `PAGE1_LABEL`, etc. with display labels
5. Replace `PAGE1_ICON_GLYPH`, etc. with glyphs from the Icon Lookup Table
6. Add or remove `NavigationViewItem` + `TabBarItem` pairs as needed
7. Both `NavigationViewItem` and `TabBarItem` for the same page MUST use the same `uen:Region.Name`
