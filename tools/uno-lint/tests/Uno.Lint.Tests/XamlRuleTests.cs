using Xunit;
using static Uno.Lint.Tests.TestHost;

namespace Uno.Lint.Tests;

/// <summary>The XAML rules, rule by rule: what each one reports and what it leaves alone.</summary>
public class XamlRuleTests
{
    // ------------------------------------------------------------ HEX

    [Fact]
    public void Hex_flags_attribute_literal()
    {
        var hit = Assert.Single(LintXaml($"<Page {Ns}><Border Background=\"#FF0000\"/></Page>"), x => x.Rule == Rules.Hex);
        Assert.Equal(1, hit.Line);
        Assert.Contains("#FF0000", hit.Message);
    }

    [Fact]
    public void Hex_flags_element_content_literal()
    {
        var hit = Assert.Single(LintXaml($"<ResourceDictionary {Ns}>\n<Color x:Key=\"Brand\">#7A67F8</Color>\n</ResourceDictionary>", "App.xaml"), x => x.Rule == Rules.Hex);
        Assert.Equal(2, hit.Line);
    }

    [Fact]
    public void Hex_flags_named_colors_but_not_transparent()
    {
        var xaml = $$"""
            <Page {{Ns}}>
              <TextBlock Foreground="Red" Text="Red"/>
              <Border Background="White" BorderBrush="Transparent"/>
              <SolidColorBrush x:Key="InkInvariantBrush">Black</SolidColorBrush>
            </Page>
            """;
        var hits = LintXaml(xaml).Where(f => f.Rule == Rules.Hex).Select(f => f.Message).ToList();
        Assert.Equal(3, hits.Count);
        Assert.Contains(hits, m => m.StartsWith("literal Red on TextBlock.Foreground"));
        Assert.Contains(hits, m => m.StartsWith("literal White on Border.Background"));
        Assert.Contains(hits, m => m.StartsWith("literal Black in <SolidColorBrush>"));
    }

    [Fact]
    public void Hex_skips_palette_files()
    {
        Assert.DoesNotContain(LintXaml($"<ResourceDictionary {Ns}><Color x:Key=\"Brand\">#7A67F8</Color></ResourceDictionary>", "Colors.xaml"), x => x.Rule == Rules.Hex);
        Assert.DoesNotContain(LintXaml($"<ResourceDictionary {Ns}><Color x:Key=\"Brand\">#7A67F8</Color></ResourceDictionary>", "ColorPaletteOverride.xaml"), x => x.Rule == Rules.Hex);
    }

    [Theory]
    [InlineData("Colors.xaml", true)]
    [InlineData("AppColors.xaml", true)]
    [InlineData("BrandPalette.Dark.xaml", true)]
    [InlineData("DesignTokens.xaml", true)]
    [InlineData("ColorPickerPage.xaml", false)]
    [InlineData("AuthTokenPage.xaml", false)]
    [InlineData("BackgroundColorPage.xaml", false)]
    public void Hex_palette_exemption_matches_whole_words_of_the_file_name(string fileName, bool exempt)
    {
        var hits = LintXaml($"<Page {Ns}><Border Background=\"#FF0000\"/></Page>", fileName).Count(x => x.Rule == Rules.Hex);
        Assert.Equal(exempt ? 0 : 1, hits);
    }

    [Fact]
    public void Hex_ignores_hex_looking_text_on_non_color_properties()
    {
        var xaml = $$"""
            <Page {{Ns}}>
              <TextBlock Text="#404" Tag="#ABC123"/>
              <x:String x:Key="Code">#BEEF</x:String>
              <Style TargetType="TextBlock"><Setter Property="Text" Value="#404"/></Style>
            </Page>
            """;
        Assert.DoesNotContain(LintXaml(xaml), x => x.Rule == Rules.Hex);
    }

    [Fact]
    public void Hex_flags_color_slots_in_setters_animations_and_gradients()
    {
        var xaml = $$"""
            <Page {{Ns}}>
              <Style TargetType="Border"><Setter Property="Background" Value="#111111"/></Style>
              <Style TargetType="Border"><Setter Property="BorderBrush"><Setter.Value>#222222</Setter.Value></Setter></Style>
              <ObjectAnimationUsingKeyFrames Storyboard.TargetProperty="(Border.Background)"><DiscreteObjectKeyFrame Value="#333333"/></ObjectAnimationUsingKeyFrames>
              <ColorAnimation To="#444444"/>
              <LinearGradientBrush><GradientStop Color="#555555" Offset="0"/></LinearGradientBrush>
              <utu:ShadowContainer><utu:ShadowContainer.Shadows><utu:ShadowCollection><utu:Shadow Color="#666666"/></utu:ShadowCollection></utu:ShadowContainer.Shadows></utu:ShadowContainer>
              <Rectangle Fill="#777777" Stroke="#888888"/>
              <VisualStateManager.VisualStateGroups><VisualStateGroup><VisualState><VisualState.Setters>
                <Setter Target="Hero.Background" Value="#999999"/>
                <Setter Target="Hero.(Border.BorderBrush)" Value="#AAAAAA"/>
                <Setter Target="Title.Text" Value="#404"/>
              </VisualState.Setters></VisualState></VisualStateGroup></VisualStateManager.VisualStateGroups>
            </Page>
            """;
        Assert.Equal(10, LintXaml(xaml).Count(x => x.Rule == Rules.Hex));
    }

    [Fact]
    public void Hex_skips_keyed_resources_inside_theme_dictionaries()
    {
        var xaml = $$"""
            <ResourceDictionary {{Ns}}>
              <ResourceDictionary.ThemeDictionaries>
                <ResourceDictionary x:Key="Light"><Color x:Key="Brand">#7A67F8</Color></ResourceDictionary>
                <ResourceDictionary x:Key="Dark"><Color x:Key="Brand">#A59CFF</Color></ResourceDictionary>
              </ResourceDictionary.ThemeDictionaries>
            </ResourceDictionary>
            """;
        Assert.DoesNotContain(LintXaml(xaml, "App.xaml"), x => x.Rule == Rules.Hex);
    }

    [Fact]
    public void Hex_exempts_children_of_keyed_resources_inside_theme_dictionaries()
    {
        var xaml = $$"""
            <ResourceDictionary {{Ns}}>
              <ResourceDictionary.ThemeDictionaries>
                <ResourceDictionary x:Key="Light">
                  <LinearGradientBrush x:Key="HeroBrush"><GradientStop Color="#FF0000" Offset="0"/></LinearGradientBrush>
                  <SolidColorBrush x:Key="InkBrush"><SolidColorBrush.Color>#222222</SolidColorBrush.Color></SolidColorBrush>
                  <Style x:Key="Pill" TargetType="Border"><Setter Property="Background" Value="#333333"/></Style>
                  <SolidColorBrush Color="#444444"/>
                </ResourceDictionary>
              </ResourceDictionary.ThemeDictionaries>
            </ResourceDictionary>
            """;
        // Only the unkeyed brush at the dictionary level is a literal with no token behind it.
        var hit = Assert.Single(LintXaml(xaml, "App.xaml"), f => f.Rule == Rules.Hex);
        Assert.Contains("#444444", hit.Message);
    }

    [Fact]
    public void Hex_ignores_non_color_hash_values_and_resource_refs()
    {
        var findings = LintXaml($"<Page {Ns}><TextBlock Text=\"#hashtag\" Foreground=\"{{ThemeResource PrimaryBrush}}\"/></Page>");
        Assert.DoesNotContain(findings, x => x.Rule == Rules.Hex);
    }

    [Fact]
    public void A_resource_key_named_like_a_color_is_not_a_color()
    {
        // <SolidColorBrush x:Key="Red">Red</SolidColorBrush>: the text is the color; the key only names the resource.
        var hit = Assert.Single(LintXaml($"<ResourceDictionary {Ns}>\n  <SolidColorBrush x:Key=\"Red\">Red</SolidColorBrush>\n</ResourceDictionary>", "App.xaml"));
        Assert.Equal("literal Red in <SolidColorBrush>", hit.Message);
    }

    // ------------------------------------------------------------ HEX and TOKENTHEME on one brush

    [Fact]
    public void A_single_themed_literal_brush_is_one_hex_finding_whatever_the_formatting()
    {
        var oneLine = LintXaml($"<ResourceDictionary {Ns}><SolidColorBrush x:Key=\"StatusBrush\" Color=\"#18D96A\"/></ResourceDictionary>", "App.xaml");
        var twoLines = LintXaml($"<ResourceDictionary {Ns}>\n<SolidColorBrush x:Key=\"StatusBrush\"\n    Color=\"#18D96A\"/>\n</ResourceDictionary>", "App.xaml");
        var textContent = LintXaml($"<ResourceDictionary {Ns}>\n<SolidColorBrush x:Key=\"StatusBrush\">#18D96A</SolidColorBrush>\n</ResourceDictionary>", "App.xaml");

        foreach (var findings in new[] { oneLine, twoLines, textContent })
        {
            var hit = Assert.Single(findings);
            Assert.Equal(Rules.Hex, hit.Rule);
            Assert.Contains("StatusBrush is also defined once for both themes", hit.Message);
        }
    }

    [Fact]
    public void Allow_hex_suppresses_the_combined_finding()
    {
        var xaml = $"<ResourceDictionary {Ns}>\n<!-- uno-lint: allow hex - status colour, fixed by design -->\n<SolidColorBrush x:Key=\"StatusBrush\" Color=\"#18D96A\"/>\n</ResourceDictionary>";
        var hit = Assert.Single(LintXaml(xaml, "App.xaml", includeSuppressed: true));
        Assert.True(hit.Suppressed);
    }

    [Fact]
    public void TokenTheme_takes_over_when_hex_is_off_for_the_file()
    {
        // In a palette file HEX does not run, so a single-themed literal brush is still a TOKENTHEME suggestion.
        var hit = Assert.Single(LintXaml($"<ResourceDictionary {Ns}><SolidColorBrush x:Key=\"StatusBrush\" Color=\"#18D96A\"/></ResourceDictionary>", "Colors.xaml"));
        Assert.Equal(Rules.TokenTheme, hit.Rule);
    }

    // ------------------------------------------------------------ TOKENTHEME

    [Fact]
    public void TokenTheme_flags_single_valued_app_brush()
    {
        // A fixed color through a StaticResource: not a literal (so not HEX), but still one value for both themes.
        var findings = LintXaml($"<ResourceDictionary {Ns}><SolidColorBrush x:Key=\"AccentBrush\" Color=\"{{StaticResource BrandRed}}\"/></ResourceDictionary>", "App.xaml");
        var hit = Assert.Single(findings, x => x.Rule == Rules.TokenTheme);
        Assert.Contains("AccentBrush", hit.Message);
    }

    [Fact]
    public void TokenTheme_skips_brush_whose_color_is_a_theme_resource()
    {
        var findings = LintXaml($"<ResourceDictionary {Ns}><SolidColorBrush x:Key=\"DividerBrush\" Color=\"{{ThemeResource OnSurfaceColor}}\" Opacity=\"0.13\"/></ResourceDictionary>", "Colors.xaml");
        Assert.DoesNotContain(findings, x => x.Rule == Rules.TokenTheme);
    }

    [Fact]
    public void TokenTheme_skips_theme_dictionaries_and_invariant_names()
    {
        var xaml = $$"""
            <ResourceDictionary {{Ns}}>
              <ResourceDictionary.ThemeDictionaries>
                <ResourceDictionary x:Key="Light"><SolidColorBrush x:Key="AccentBrush" Color="Red"/></ResourceDictionary>
              </ResourceDictionary.ThemeDictionaries>
              <SolidColorBrush x:Key="ScrimInvariantBrush" Color="Black"/>
              <SolidColorBrush x:Key="InkOnLightBrush" Color="Black"/>
            </ResourceDictionary>
            """;
        Assert.DoesNotContain(LintXaml(xaml, "App.xaml"), x => x.Rule == Rules.TokenTheme);
    }

    [Fact]
    public void TokenTheme_skips_static_and_theme_resource_aliases()
    {
        var xaml = $$"""
            <ResourceDictionary {{Ns}}>
              <StaticResource x:Key="AccentAliasBrush" ResourceKey="PrimaryBrush" />
              <ThemeResource x:Key="SurfaceAliasBrush" ResourceKey="SurfaceBrush" />
              <SolidColorBrush x:Key="BrandBlueBrush" Color="{StaticResource BrandBlue}" />
            </ResourceDictionary>
            """;
        var hit = Assert.Single(LintXaml(xaml, "App.xaml"), f => f.Rule == Rules.TokenTheme);
        Assert.Contains("BrandBlueBrush", hit.Message);
    }

    // ------------------------------------------------------------ ICON

    [Theory]
    [InlineData("Icons.xaml", true)]
    [InlineData("MaterialIcons.xaml", true)]
    [InlineData("LexiconPage.xaml", false)]
    [InlineData("IconButtonPage.xaml", false)]
    public void Icon_file_exemption_matches_whole_words_of_the_file_name(string fileName, bool exempt)
    {
        var hits = LintXaml($"<Page {Ns}><Path Data=\"M0,0 L1,1\"/></Page>", fileName).Count(x => x.Rule == Rules.Icon);
        Assert.Equal(exempt ? 0 : 1, hits);
    }

    [Fact]
    public void Icon_flags_inline_path_data_and_shapes_in_viewbox()
    {
        var xaml = $$"""
            <Page {{Ns}}>
              <PathIcon Data="M 0 0 L 10 10"/>
              <Path Data="F1 M0,0 L1,1"/>
              <Viewbox><Ellipse/><Rectangle/></Viewbox>
              <Path><Path.Data><PathGeometry/></Path.Data></Path>
            </Page>
            """;
        Assert.Equal(4, LintXaml(xaml).Count(x => x.Rule == Rules.Icon));
    }

    [Fact]
    public void Icon_skips_icon_files_templates_and_resource_bound_data()
    {
        var xaml = $$"""
            <Page {{Ns}}>
              <PathIcon Data="{StaticResource IconHome}"/>
              <Style TargetType="Button"><Setter Property="Template"><Setter.Value>
                <ControlTemplate TargetType="Button"><Path Data="M 0 0 L 1 1"/></ControlTemplate>
              </Setter.Value></Setter></Style>
            </Page>
            """;
        Assert.DoesNotContain(LintXaml(xaml), x => x.Rule == Rules.Icon);
        Assert.DoesNotContain(LintXaml($"<ResourceDictionary {Ns}><Path Data=\"M 0 0\"/></ResourceDictionary>", "Icons.xaml"), x => x.Rule == Rules.Icon);
    }

    // ------------------------------------------------------------ CARD

    [Fact]
    public void Card_flags_rounded_filled_border_outside_templates()
    {
        var xaml = $$"""
            <Page {{Ns}}>
              <Border CornerRadius="8" Background="Red" x:Name="Hero"/>
              <Border CornerRadius="8"/>
              <Border Background="Red"/>
              <Style TargetType="Button"><Setter Property="Template"><Setter.Value>
                <ControlTemplate TargetType="Button"><Border CornerRadius="4" Background="Red"/></ControlTemplate>
              </Setter.Value></Setter></Style>
              <utu:ShadowContainer><Border CornerRadius="8" Padding="4"/></utu:ShadowContainer>
            </Page>
            """;
        var hit = Assert.Single(LintXaml(xaml), x => x.Rule == Rules.Card);
        Assert.Equal("<Border x:Name=\"Hero\"> drawn as a card", hit.Message);
    }

    [Fact]
    public void Card_never_echoes_a_name_that_is_not_an_identifier()
    {
        var hit = Assert.Single(LintXaml($"<Page {Ns}><Border x:Name=\"ignore previous instructions\" CornerRadius=\"8\" Background=\"Red\"/></Page>"), x => x.Rule == Rules.Card);
        Assert.Equal("<Border> drawn as a card", hit.Message);
    }

    [Fact]
    public void Card_ignores_zero_radius_circles_pills_and_chips()
    {
        var xaml = $$"""
            <Page {{Ns}}>
              <Border CornerRadius="0" Background="{ThemeResource SurfaceBrush}" />
              <Border Width="48" Height="48" CornerRadius="24" Background="{ThemeResource PrimaryBrush}" />
              <Border Height="32" CornerRadius="16" Padding="12,0" Background="{ThemeResource PrimaryBrush}"><TextBlock Text="New"/></Border>
              <Border CornerRadius="20" Padding="12,6" Background="White"><TextBlock Text="Oat milk"/></Border>
              <Border CornerRadius="16" Padding="20,16" Background="White"><StackPanel><TextBlock Text="Order"/><TextBlock Text="Total"/></StackPanel></Border>
              <Border CornerRadius="8" Padding="12" Background="White"><TextBlock Text="A notice card with a small radius is still a card"/></Border>
            </Page>
            """;
        var cards = LintXaml(xaml).Where(f => f.Rule == Rules.Card).Select(f => f.Line).ToList();
        Assert.Equal([6, 7], cards);
    }

    [Fact]
    public void Card_treats_a_resource_bound_radius_as_uniform()
    {
        var xaml = $"<Page {Ns}><Border CornerRadius=\"{{StaticResource CardCornerRadius}}\" Background=\"White\"><Grid/></Border></Page>";
        Assert.Single(LintXaml(xaml), f => f.Rule == Rules.Card);
    }

    [Fact]
    public void Card_skips_partial_corner_radius_swatches()
    {
        // A grid of colour swatches with rounded outer corners only.
        var xaml = $$"""
            <Page {{Ns}}>
              <Border Background="{ThemeResource PrimaryBrush}" CornerRadius="8,0,0,0" Padding="8"/>
              <Border Background="{ThemeResource PrimaryBrush}" CornerRadius="0,0,8,0" Padding="8"/>
              <Border Background="{ThemeResource PrimaryBrush}" CornerRadius="8" Padding="8"/>
              <Border Background="{ThemeResource PrimaryBrush}" CornerRadius="8,8,8,8" Padding="8"/>
            </Page>
            """;
        Assert.Equal(2, LintXaml(xaml).Count(f => f.Rule == Rules.Card));
    }

    [Fact]
    public void Card_and_BackBar_are_off_without_toolkit_or_in_recommended_profile()
    {
        var xaml = $"<Page {Ns}><Border CornerRadius=\"8\" Background=\"Red\"/><Button uen:Navigation.Request=\"-\"/></Page>";
        Assert.DoesNotContain(LintXaml(xaml, toolkit: false), x => x.Rule == Rules.Card || x.Rule == Rules.BackBar);
        Assert.DoesNotContain(LintXaml(xaml, profile: Profile.Recommended), x => x.Rule == Rules.Card || x.Rule == Rules.BackBar);
        Assert.Equal(2, LintXaml(xaml).Count(x => x.Rule == Rules.Card || x.Rule == Rules.BackBar));
    }

    // ------------------------------------------------------------ BACKBAR

    [Fact]
    public void BackBar_skips_pages_with_a_navigation_bar()
    {
        Assert.DoesNotContain(LintXaml($"<Page {Ns}><utu:NavigationBar/><Button uen:Navigation.Request=\"-\"/></Page>"), x => x.Rule == Rules.BackBar);
    }

    [Fact]
    public void BackBar_waits_for_a_navigation_bar_after_the_back_request()
    {
        Assert.DoesNotContain(LintXaml($"<Page {Ns}><Button uen:Navigation.Request=\"-\"/><utu:NavigationBar/></Page>"), x => x.Rule == Rules.BackBar);
    }

    // ------------------------------------------------------------ BINDVALUE

    [Fact]
    public void BindValue_flags_state_dot_value_in_mvux_projects_only()
    {
        var xaml = $$"""
            <Page {{Ns}}>
              <TextBox Text="{Binding Name.Value, Mode=TwoWay}" />
              <TextBlock Text="{Binding Path=Profile.Address.Value}" />
              <TextBlock Text="{Binding Name, Mode=TwoWay}" />
              <TextBlock Text="{Binding Value}" />
              <ListView ItemsSource="{Binding Items}"><ListView.ItemTemplate><DataTemplate><TextBlock Text="{Binding Pair.Value}"/></DataTemplate></ListView.ItemTemplate></ListView>
            </Page>
            """;
        var mvux = LintXaml(xaml, mvux: true).Where(f => f.Rule == Rules.BindValue).ToList();
        Assert.Equal(2, mvux.Count);
        Assert.Contains(mvux, f => f.Line == 2 && f.Message.Contains("Name.Value") && f.Message.Contains("bind Name itself"));
        Assert.Contains(mvux, f => f.Line == 3 && f.Message.Contains("Profile.Address.Value"));

        Assert.DoesNotContain(LintXaml(xaml, mvux: false), f => f.Rule == Rules.BindValue);
    }

    [Fact]
    public void BindValue_is_a_recommended_correctness_rule_gated_on_mvux()
    {
        Assert.Equal(RuleCategory.Correctness, Rules.BindValue.Category);
        Assert.Equal(Severity.Warning, Rules.Resolve(Rules.BindValue, Profile.Recommended, new ProjectContext(hasMvux: true)));
        Assert.Equal(Severity.None, Rules.Resolve(Rules.BindValue, Profile.Recommended, ProjectContext.Empty));
    }

    // ------------------------------------------------------------ order

    [Fact]
    public void Findings_come_in_line_order()
    {
        // TOKENTHEME and CARD are decided at end tags, after the HEX hits further down; the report still reads top to bottom.
        var xaml = $$"""
            <Page {{Ns}}>
              <Page.Resources><SolidColorBrush x:Key="AccentBrush" Color="{StaticResource Brand}"/></Page.Resources>
              <Border CornerRadius="8" Background="{ThemeResource SurfaceBrush}">
                <Border Background="#FF0000"/>
              </Border>
            </Page>
            """;
        var lines = LintXaml(xaml).Select(f => f.Line).ToList();
        Assert.Equal(lines.OrderBy(l => l), lines);
        Assert.Equal(3, lines.Count);
    }
}
