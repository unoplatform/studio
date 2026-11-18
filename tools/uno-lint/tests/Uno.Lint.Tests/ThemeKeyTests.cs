using Xunit;
using static Uno.Lint.Tests.TestHost;

namespace Uno.Lint.Tests;

/// <summary>THEMEKEY (UNOL202): lightweight-styling overrides no style the app gets reads, and the theme detection it needs.</summary>
public class ThemeKeyTests
{
    private static readonly ProjectContext Simple = new(hasToolkit: true, themes: UnoThemeKinds.Simple);
    private static readonly ProjectContext Material = new(hasToolkit: true, themes: UnoThemeKinds.Material);

    private static string Dictionary(string entries) => $$"""
        <ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
          <ResourceDictionary.ThemeDictionaries>
            <ResourceDictionary x:Key="Light">
        {{entries}}
            </ResourceDictionary>
          </ResourceDictionary.ThemeDictionaries>
        </ResourceDictionary>
        """;

    private static List<Finding> ThemeKeys(string xaml, ProjectContext context, string fileName = "Styles/Brand.xaml") =>
        LintXaml(xaml, context, fileName).Where(f => f.Rule == Rules.ThemeKey).ToList();

    [Fact]
    public void Flags_a_WinUI_key_that_Simple_renamed_and_names_the_real_one()
    {
        var xaml = Dictionary("""
                  <SolidColorBrush x:Key="ListViewItemBackgroundSelected" Color="#DCEFED" />
                  <SolidColorBrush x:Key="ListViewItemBackgroundSelectedPointerOver" Color="#DCEFED" />
                  <SolidColorBrush x:Key="ListViewItemBackgroundPressedSelected" Color="#CFE3E1" />
            """);

        var hit = Assert.Single(ThemeKeys(xaml, Simple));
        Assert.Equal(5, hit.Line);
        Assert.Equal("ListViewItemBackgroundSelectedPointerOver is not read by Simple's ListViewItem style, so this override changes nothing; did you mean ListViewItemBackgroundPointerOverSelected?", hit.Message);
    }

    [Fact]
    public void The_same_key_is_fine_on_WinUI_styles()
    {
        var xaml = Dictionary("""<SolidColorBrush x:Key="ListViewItemBackgroundSelectedPointerOver" Color="#DCEFED" />""");
        Assert.Empty(ThemeKeys(xaml, ProjectContext.Empty));
    }

    [Fact]
    public void A_theme_that_only_sets_some_WinUI_keys_leaves_the_rest_read()
    {
        // Simple's NavigationViewItem style sets a Foreground; WinUI's template still reads its other keys.
        var xaml = Dictionary("""<x:Double x:Key="NavigationViewItemOnLeftMinHeight">48</x:Double>""");
        Assert.Empty(ThemeKeys(xaml, Simple));
    }

    [Fact]
    public void Material_buttons_read_the_variant_keys()
    {
        var hit = Assert.Single(ThemeKeys(Dictionary("""<SolidColorBrush x:Key="ButtonBackground" Color="#0E5C58" />"""), Material));
        Assert.EndsWith("did you mean FilledButtonBackground?", hit.Message);
    }

    [Fact]
    public void A_style_based_on_the_WinUI_style_reads_the_WinUI_keys()
    {
        // The Toolkit's ResourceExtensions sample: a Material app, a Button style BasedOn WinUI's DefaultButtonStyle.
        var xaml = $$"""
            <Page {{Ns}}>
              <Page.Resources>
                <Style x:Key="Overridden" TargetType="Button" BasedOn="{StaticResource DefaultButtonStyle}">
                  <Setter Property="utu:ResourceExtensions.Resources">
                    <Setter.Value>
                      <ResourceDictionary><SolidColorBrush x:Key="ButtonBackground" Color="DarkGreen" /></ResourceDictionary>
                    </Setter.Value>
                  </Setter>
                </Style>
              </Page.Resources>
            </Page>
            """;
        Assert.Empty(ThemeKeys(xaml, Material, "MainPage.xaml"));
    }

    [Fact]
    public void A_theme_of_unknown_kind_keeps_every_candidate()
    {
        var xaml = Dictionary("""<SolidColorBrush x:Key="ListViewItemBackgroundSelectedPointerOver" Color="#DCEFED" />""");
        Assert.Empty(ThemeKeys(xaml, new ProjectContext(hasUnoThemes: true)));
    }

    [Theory]
    [InlineData("""<SolidColorBrush x:Key="CwSurface" Color="#F6F7F5" />""")] // not a control family's key
    [InlineData("""<Style x:Key="ListViewItemRowStyle" TargetType="ListViewItem" />""")] // a style, not an override
    [InlineData("""<SolidColorBrush x:Key="ToggleSwitchHalo" Color="#F6F7F5" /><Border Background="{ThemeResource ToggleSwitchHalo}" />""")] // the app's own key
    public void Leaves_app_resources_alone(string entries)
    {
        Assert.Empty(ThemeKeys(Dictionary(entries), Simple));
    }

    [Fact]
    public void Checks_overrides_in_a_page_or_style_too_and_honours_allow_comments()
    {
        var xaml = $$"""
            <Page {{Ns}}>
              <Page.Resources>
                <SolidColorBrush x:Key="ToggleSwitchFillOn" Color="#0E5C58" />
                <!-- uno-lint: allow themekey - kept for the WinUI head -->
                <SolidColorBrush x:Key="ToggleSwitchFillOnPointerOver" Color="#0E5C58" />
              </Page.Resources>
            </Page>
            """;
        var hits = LintXaml(xaml, Simple, includeSuppressed: true).Where(f => f.Rule == Rules.ThemeKey).ToList();
        Assert.Equal(2, hits.Count);
        Assert.False(hits[0].Suppressed);
        Assert.True(hits[1].Suppressed);
    }

    [Fact]
    public void Is_a_recommended_correctness_rule()
    {
        Assert.Equal(RuleCategory.Correctness, Rules.ThemeKey.Category);
        Assert.Equal(Severity.Warning, Rules.Resolve(Rules.ThemeKey, Profile.Recommended, ProjectContext.Empty));
    }

    // ------------------------------------------------------------ which theme

    [Theory]
    [InlineData("<UnoFeatures>SimpleTheme;Toolkit</UnoFeatures>", UnoThemeKinds.Simple)]
    [InlineData("<UnoFeatures>MaterialToolkit</UnoFeatures>", UnoThemeKinds.Material)]
    [InlineData("<UnoFeatures>Cupertino;Material</UnoFeatures>", UnoThemeKinds.Material | UnoThemeKinds.Cupertino)]
    [InlineData("<PackageReference Include=\"Uno.Simple.WinUI\" />", UnoThemeKinds.Simple)]
    [InlineData("<PackageReference Include=\"Uno.Toolkit.WinUI.Material\" />", UnoThemeKinds.Material)]
    [InlineData("<UnoFeatures>Themes</UnoFeatures>", UnoThemeKinds.None)]
    public void Context_names_the_theme(string projectXml, UnoThemeKinds kinds)
    {
        var context = ProjectContext.Detect([$"<Project>{projectXml}</Project>"], []);
        Assert.True(context.HasUnoThemes);
        Assert.Equal(kinds, context.Themes);
    }

    [Fact]
    public void Context_names_the_theme_from_xaml_namespaces()
    {
        var context = ProjectContext.Detect([], ["<Application xmlns:utus=\"using:Uno.Toolkit.UI.Simple\"/>"]);
        Assert.Equal(UnoThemeKinds.Simple, context.Themes);
    }

    [Fact]
    public void Cache_form_keeps_the_theme_and_rejects_the_old_form()
    {
        var context = new ProjectContext(hasToolkit: true, hasMvux: true, themes: UnoThemeKinds.Simple | UnoThemeKinds.Material);
        Assert.True(ProjectContext.TryDeserialize(context.Serialize(), out var read));
        Assert.Equal(context.Themes, read.Themes);
        Assert.True(read.HasUnoThemes && read.HasToolkit && read.HasMvux);
        Assert.False(ProjectContext.TryDeserialize("T-MH", out _));
    }
}
