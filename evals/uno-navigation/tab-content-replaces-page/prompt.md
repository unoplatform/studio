---
description: A TabBar shell whose content area is a StackPanel of collapsed pages is repaired into an empty Grid visibility region.
tags: [uno-navigation, regions]
runs: 3
max_turns: 10
plugins: ["../../../plugins/uno-platform-studio"]
allowed_tools: [Skill, Read, Glob, Grep]
---
My Uno Platform app (Uno.Extensions navigation + Toolkit) has a MainPage with a bottom TabBar. Tapping a tab replaces the whole MainPage with the tab's page, so the tab bar disappears. Here is MainPage.xaml:

```xml
<Page x:Class="Shop.Presentation.MainPage"
      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      xmlns:local="using:Shop.Presentation"
      xmlns:uen="using:Uno.Extensions.Navigation.UI"
      xmlns:utu="using:Uno.Toolkit.UI">
  <Grid uen:Region.Attached="True">
    <Grid.RowDefinitions>
      <RowDefinition Height="*" />
      <RowDefinition Height="Auto" />
    </Grid.RowDefinitions>
    <StackPanel uen:Region.Attached="True" uen:Region.Navigator="Visibility">
      <local:HomePage />
      <local:CartPage Visibility="Collapsed" />
      <local:ProfilePage Visibility="Collapsed" />
    </StackPanel>
    <utu:TabBar Grid.Row="1" uen:Region.Attached="True">
      <utu:TabBarItem Content="Home" uen:Region.Name="Home" />
      <utu:TabBarItem Content="Cart" uen:Region.Name="Cart" />
      <utu:TabBarItem Content="Profile" uen:Region.Name="Profile" />
    </utu:TabBar>
  </Grid>
</Page>
```

Routes in App.xaml.cs register Home, Cart and Profile as nested routes of Main. What's wrong and what should the XAML be?
