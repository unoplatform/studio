using Uno.Lint;
using Xunit;

namespace Uno.Lint.Tests;

public class XamlRuleTests
{
    private const string Ns = "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" xmlns:utu=\"using:Uno.Toolkit.UI\"";

    private static List<Finding> Lint(string xaml, string fileName = "MainPage.xaml", bool toolkit = true, Profile profile = Profile.Strict)
    {
        Assert.True(XamlFile.TryParse(fileName, xaml, out var file), "test XAML must parse");
        var ctx = new ProjectContext { HasToolkit = toolkit };
        var options = new LintOptions { Profile = profile };
        return XamlAnalyzer.Analyze(file!, ctx, r => Rules.Resolve(r, options, ctx) != Severity.None).ToList();
    }

    // ------------------------------------------------------------ HEX

    [Fact]
    public void Hex_flags_attribute_literal()
    {
        var f = Lint($"<Page {Ns}><Border Background=\"#FF0000\"/></Page>");
        var hit = Assert.Single(f, x => x.Rule == Rules.Hex);
        Assert.Equal(1, hit.Line);
        Assert.Contains("#FF0000", hit.Message);
    }

    [Fact]
    public void Hex_flags_element_content_literal()
    {
        var f = Lint($"<ResourceDictionary {Ns}>\n<Color x:Key=\"Brand\">#7A67F8</Color>\n</ResourceDictionary>", "App.xaml");
        var hit = Assert.Single(f, x => x.Rule == Rules.Hex);
        Assert.Equal(2, hit.Line);
    }

    [Fact]
    public void Hex_skips_palette_files()
    {
        Assert.Empty(Lint($"<ResourceDictionary {Ns}><Color x:Key=\"Brand\">#7A67F8</Color></ResourceDictionary>", "Colors.xaml").Where(x => x.Rule == Rules.Hex));
        Assert.Empty(Lint($"<ResourceDictionary {Ns}><Color x:Key=\"Brand\">#7A67F8</Color></ResourceDictionary>", "ColorPaletteOverride.xaml").Where(x => x.Rule == Rules.Hex));
    }

    [Fact]
    public void Hex_skips_keyed_resources_inside_theme_dictionaries()
    {
        var xaml = $@"<ResourceDictionary {Ns}>
  <ResourceDictionary.ThemeDictionaries>
    <ResourceDictionary x:Key=""Light""><Color x:Key=""Brand"">#7A67F8</Color></ResourceDictionary>
    <ResourceDictionary x:Key=""Dark""><Color x:Key=""Brand"">#A59CFF</Color></ResourceDictionary>
  </ResourceDictionary.ThemeDictionaries>
</ResourceDictionary>";
        Assert.Empty(Lint(xaml, "App.xaml").Where(x => x.Rule == Rules.Hex));
    }

    [Fact]
    public void Hex_ignores_non_color_hash_values_and_resource_refs()
    {
        var f = Lint($"<Page {Ns}><TextBlock Text=\"#hashtag\" Foreground=\"{{ThemeResource PrimaryBrush}}\"/></Page>");
        Assert.Empty(f.Where(x => x.Rule == Rules.Hex));
    }

    [Fact]
    public void Hex_honours_suppression_comment_with_reason()
    {
        var xaml = $"<Page {Ns}>\n<!-- uno-lint: allow hex - chart series colour -->\n<Border Background=\"#FF0000\"/>\n</Page>";
        Assert.Empty(Lint(xaml).Where(x => x.Rule == Rules.Hex));

        var legacy = $"<Page {Ns}>\n<!-- xaml-lint: allow hex - chart series colour -->\n<Border Background=\"#FF0000\"/>\n</Page>";
        Assert.Empty(Lint(legacy).Where(x => x.Rule == Rules.Hex));

        var noReason = $"<Page {Ns}>\n<!-- uno-lint: allow hex -->\n<Border Background=\"#FF0000\"/>\n</Page>";
        Assert.Single(Lint(noReason).Where(x => x.Rule == Rules.Hex));
    }

    // ------------------------------------------------------------ TOKENTHEME

    [Fact]
    public void TokenTheme_flags_single_valued_app_brush()
    {
        var f = Lint($"<ResourceDictionary {Ns}><SolidColorBrush x:Key=\"AccentBrush\" Color=\"Red\"/></ResourceDictionary>", "App.xaml");
        var hit = Assert.Single(f, x => x.Rule == Rules.TokenTheme);
        Assert.Contains("AccentBrush", hit.Message);
    }

    [Fact]
    public void TokenTheme_skips_brush_whose_color_is_a_theme_resource()
    {
        // The PowerShell prototype flagged this in the Toolkit's own Colors.xaml; it follows the theme.
        var f = Lint($"<ResourceDictionary {Ns}><SolidColorBrush x:Key=\"DividerBrush\" Color=\"{{ThemeResource OnSurfaceColor}}\" Opacity=\"0.13\"/></ResourceDictionary>", "Colors.xaml");
        Assert.Empty(f.Where(x => x.Rule == Rules.TokenTheme));
    }

    [Fact]
    public void TokenTheme_skips_theme_dictionaries_and_invariant_names()
    {
        var xaml = $@"<ResourceDictionary {Ns}>
  <ResourceDictionary.ThemeDictionaries>
    <ResourceDictionary x:Key=""Light""><SolidColorBrush x:Key=""AccentBrush"" Color=""Red""/></ResourceDictionary>
  </ResourceDictionary.ThemeDictionaries>
  <SolidColorBrush x:Key=""ScrimInvariantBrush"" Color=""Black""/>
  <SolidColorBrush x:Key=""InkOnLightBrush"" Color=""Black""/>
</ResourceDictionary>";
        Assert.Empty(Lint(xaml, "App.xaml").Where(x => x.Rule == Rules.TokenTheme));
    }

    // ------------------------------------------------------------ ICON

    [Fact]
    public void Icon_flags_inline_path_data_and_shapes_in_viewbox()
    {
        var xaml = $@"<Page {Ns}>
  <PathIcon Data=""M 0 0 L 10 10""/>
  <Path Data=""F1 M0,0 L1,1""/>
  <Viewbox><Ellipse/><Rectangle/></Viewbox>
  <Path><Path.Data><PathGeometry/></Path.Data></Path>
</Page>";
        var hits = Lint(xaml).Where(x => x.Rule == Rules.Icon).ToList();
        Assert.Equal(4, hits.Count);
    }

    [Fact]
    public void Icon_skips_icon_files_templates_and_resource_bound_data()
    {
        var xaml = $@"<Page {Ns}>
  <PathIcon Data=""{{StaticResource IconHome}}""/>
  <Style TargetType=""Button""><Setter Property=""Template""><Setter.Value>
    <ControlTemplate TargetType=""Button""><Path Data=""M 0 0 L 1 1""/></ControlTemplate>
  </Setter.Value></Setter></Style>
</Page>";
        Assert.Empty(Lint(xaml).Where(x => x.Rule == Rules.Icon));
        Assert.Empty(Lint($"<ResourceDictionary {Ns}><Path Data=\"M 0 0\"/></ResourceDictionary>", "Icons.xaml").Where(x => x.Rule == Rules.Icon));
    }

    // ------------------------------------------------------------ CARD

    [Fact]
    public void Card_flags_rounded_filled_border_outside_templates()
    {
        var xaml = $@"<Page {Ns}>
  <Border CornerRadius=""8"" Background=""Red"" x:Name=""Hero""/>
  <Border CornerRadius=""8""/>
  <Border Background=""Red""/>
  <Style TargetType=""Button""><Setter Property=""Template""><Setter.Value>
    <ControlTemplate TargetType=""Button""><Border CornerRadius=""4"" Background=""Red""/></ControlTemplate>
  </Setter.Value></Setter></Style>
  <utu:ShadowContainer><Border CornerRadius=""8"" Padding=""4""/></utu:ShadowContainer>
</Page>";
        var hit = Assert.Single(Lint(xaml).Where(x => x.Rule == Rules.Card));
        Assert.Contains("Hero", hit.Message);
    }

    [Fact]
    public void Card_and_BackBar_are_off_without_toolkit_or_in_recommended_profile()
    {
        var xaml = $"<Page {Ns}><Border CornerRadius=\"8\" Background=\"Red\"/><Button uen:Navigation.Request=\"-\" xmlns:uen=\"using:Uno.Extensions.Navigation.UI\"/></Page>";
        Assert.Empty(Lint(xaml, toolkit: false).Where(x => x.Rule == Rules.Card || x.Rule == Rules.BackBar));
        Assert.Empty(Lint(xaml, profile: Profile.Recommended).Where(x => x.Rule == Rules.Card || x.Rule == Rules.BackBar));
        Assert.Equal(2, Lint(xaml).Count(x => x.Rule == Rules.Card || x.Rule == Rules.BackBar));
    }

    // ------------------------------------------------------------ BACKBAR

    [Fact]
    public void BackBar_skips_pages_with_a_navigation_bar()
    {
        var xaml = $"<Page {Ns} xmlns:uen=\"using:Uno.Extensions.Navigation.UI\"><utu:NavigationBar/><Button uen:Navigation.Request=\"-\"/></Page>";
        Assert.Empty(Lint(xaml).Where(x => x.Rule == Rules.BackBar));
    }

    // ------------------------------------------------------------ parsing

    [Fact]
    public void Malformed_xaml_is_reported_not_thrown()
    {
        Assert.False(XamlFile.TryParse("Bad.xaml", "<Page><Border></Page>", out _));
    }
}
