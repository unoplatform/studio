using Uno.Lint;
using Xunit;

namespace Uno.Lint.Tests;

/// <summary>Regression tests for the PR review on the first prototype: each confirmed bug and each gap raised there.</summary>
public class ReviewFixTests : IDisposable
{
    private const string Ns = "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" xmlns:utu=\"using:Uno.Toolkit.UI\"";

    private readonly string _root = Path.Combine(Path.GetTempPath(), "uno-lint-tests", Guid.NewGuid().ToString("N"));

    public ReviewFixTests()
    {
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch { }
    }

    private void Write(string relative, string content)
    {
        var path = Path.Combine(_root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }

    private static List<Finding> LintXaml(string xaml, string fileName = "MainPage.xaml", bool toolkit = true, bool mvux = false, bool includeSuppressed = false)
    {
        Assert.True(XamlFile.TryParse(fileName, xaml, out var file), "test XAML must parse");
        var ctx = new ProjectContext { HasToolkit = toolkit, HasMvux = mvux };
        var options = new LintOptions { Profile = Profile.Strict };
        return XamlAnalyzer.Analyze(file!, ctx, r => Rules.Resolve(r, options, ctx) != Severity.None).Where(f => includeSuppressed || !f.Suppressed).ToList();
    }

    private static List<Finding> LintCs(string code, string fileName = "MainPage.xaml.cs")
    {
        var ctx = new ProjectContext { HasToolkit = true };
        var options = new LintOptions { Profile = Profile.Strict };
        return CSharpAnalyzer.Analyze(new CSharpFile(fileName, code, null), ctx, r => Rules.Resolve(r, options, ctx) != Severity.None).Where(f => !f.Suppressed).ToList();
    }

    // ------------------------------------------------------------ .editorconfig sections (confirmed)

    [Fact]
    public void Editorconfig_sections_apply_only_to_matching_files()
    {
        Write(".editorconfig", "root = true\n[App/Tests/**]\ndotnet_diagnostic.UNOL001.severity = none\n[*.xaml.cs]\ndotnet_diagnostic.UNOL103.severity = suggestion\n");
        Write("App.csproj", "<Project Sdk=\"Uno.Sdk\"><PropertyGroup><UnoFeatures>Toolkit</UnoFeatures></PropertyGroup></Project>");
        var page = "<Page xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><Border Background=\"#FF0000\"/></Page>";
        Write("App/Views/Main.xaml", page);
        Write("App/Tests/Probe.xaml", page);
        Write("App/Views/Main.xaml.cs", "partial class Main { void H(object sender, EventArgs e) { } }");

        var options = new LintOptions { Profile = Profile.Strict, IncludeDev = true };
        EditorConfig.Apply(_root, options, profileExplicit: true);
        var result = new Linter(options).Run(_root);

        // The probe's HEX is off; the real page's HEX is not.
        var hex = result.Findings.Where(f => f.Rule == Rules.Hex).ToList();
        var hit = Assert.Single(hex);
        Assert.Equal(Path.Combine("App", "Views", "Main.xaml"), hit.File);

        // CODEBEHIND stays on but at suggestion severity, so it no longer gates.
        var cb = Assert.Single(result.Findings.Where(f => f.Rule == Rules.CodeBehind));
        Assert.Equal(Severity.Suggestion, cb.Severity);
        Assert.True(result.HasGatingFindings); // the HEX warning still gates
    }

    [Theory]
    [InlineData("*", "App/Views/Main.xaml", true)]
    [InlineData("*.xaml", "App/Views/Main.xaml", true)]
    [InlineData("*.xaml", "App/Views/Main.xaml.cs", false)]
    [InlineData("App/Tests/**", "App/Tests/Deep/Probe.xaml", true)]
    [InlineData("App/Tests/**", "App/Views/Main.xaml", false)]
    [InlineData("App/*.xaml", "App/Main.xaml", true)]
    [InlineData("App/*.xaml", "App/Views/Main.xaml", false)]
    [InlineData("**/Views/*.{xaml,cs}", "App/Views/Main.cs", true)]
    [InlineData("Main.xaml", "App/Views/Main.xaml", true)]
    public void Editorconfig_globs_follow_editorconfig_semantics(string glob, string relativeFile, bool matches)
    {
        var set = new EditorConfigSet();
        set.AddText(new[] { $"[{glob}]", "dotnet_diagnostic.UNOL001.severity = none" }, _root);
        var found = set.TryGetSeverity("UNOL001", Path.Combine(_root, relativeFile), out var severity);
        Assert.Equal(matches, found);
        if (matches) { Assert.Equal(Severity.None, severity); }
    }

    [Fact]
    public void Editorconfig_lists_sectioned_rule_ids_for_the_report()
    {
        var set = new EditorConfigSet();
        set.AddText(new[] { "dotnet_diagnostic.UNOL002.severity = suggestion", "[*.xaml]", "dotnet_diagnostic.UNOL001.severity = none" }, _root);
        Assert.Equal(new[] { "UNOL001" }, set.SectionedRuleIds());
        Assert.Equal(Profile.Recommended, new LintOptions().Profile);
    }

    // ------------------------------------------------------------ suppression anchor (confirmed)

    [Fact]
    public void Suppression_above_a_multiline_element_covers_its_attributes()
    {
        var xaml = $@"<Page {Ns}>
  <!-- uno-lint: allow hex - brand chart color -->
  <Border
      Width=""100""
      Height=""100""
      Margin=""4""
      Background=""#FF7A67F8"" />
  <Border Background=""#FF000000"" />
</Page>";
        var all = LintXaml(xaml, includeSuppressed: true).Where(f => f.Rule == Rules.Hex).ToList();
        Assert.Equal(2, all.Count);
        Assert.True(all.Single(f => f.Line == 7).Suppressed);
        Assert.False(all.Single(f => f.Line == 8).Suppressed);
    }

    // ------------------------------------------------------------ CARD geometry (confirmed)

    [Fact]
    public void Card_ignores_zero_radius_circles_pills_and_chips()
    {
        var xaml = $@"<Page {Ns}>
  <Border CornerRadius=""0"" Background=""{{ThemeResource SurfaceBrush}}"" />
  <Border Width=""48"" Height=""48"" CornerRadius=""24"" Background=""{{ThemeResource PrimaryBrush}}"" />
  <Border Height=""32"" CornerRadius=""16"" Padding=""12,0"" Background=""{{ThemeResource PrimaryBrush}}""><TextBlock Text=""New""/></Border>
  <Border CornerRadius=""20"" Padding=""12,6"" Background=""White""><TextBlock Text=""Oat milk""/></Border>
  <Border CornerRadius=""16"" Padding=""20,16"" Background=""White""><StackPanel><TextBlock Text=""Order""/><TextBlock Text=""Total""/></StackPanel></Border>
  <Border CornerRadius=""8"" Padding=""12"" Background=""White""><TextBlock Text=""A notice card with a small radius is still a card""/></Border>
</Page>";
        var cards = LintXaml(xaml).Where(f => f.Rule == Rules.Card).Select(f => f.Line).OrderBy(l => l).ToList();
        Assert.Equal(new[] { 6, 7 }, cards);
    }

    [Fact]
    public void Card_treats_a_resource_bound_radius_as_uniform()
    {
        var xaml = $"<Page {Ns}><Border CornerRadius=\"{{StaticResource CardCornerRadius}}\" Background=\"White\"><Grid/></Border></Page>";
        Assert.Single(LintXaml(xaml).Where(f => f.Rule == Rules.Card));
    }

    // ------------------------------------------------------------ TOKENTHEME alias (confirmed)

    [Fact]
    public void TokenTheme_skips_static_and_theme_resource_aliases()
    {
        var xaml = $@"<ResourceDictionary {Ns}>
  <StaticResource x:Key=""AccentAliasBrush"" ResourceKey=""PrimaryBrush"" />
  <ThemeResource x:Key=""SurfaceAliasBrush"" ResourceKey=""SurfaceBrush"" />
  <SolidColorBrush x:Key=""BrandBlueBrush"" Color=""#0000FF"" />
</ResourceDictionary>";
        var hit = Assert.Single(LintXaml(xaml, "App.xaml").Where(f => f.Rule == Rules.TokenTheme));
        Assert.Contains("BrandBlueBrush", hit.Message);
    }

    // ------------------------------------------------------------ named colors (coverage gap)

    [Fact]
    public void Hex_flags_named_colors_in_xaml_but_not_transparent()
    {
        var xaml = $@"<Page {Ns}>
  <TextBlock Foreground=""Red"" Text=""Red""/>
  <Border Background=""White"" BorderBrush=""Transparent""/>
  <SolidColorBrush x:Key=""InkInvariantBrush"">Black</SolidColorBrush>
</Page>";
        var hits = LintXaml(xaml).Where(f => f.Rule == Rules.Hex).Select(f => f.Message).ToList();
        Assert.Equal(3, hits.Count);
        Assert.Contains(hits, m => m.StartsWith("literal Red on TextBlock.Foreground"));
        Assert.Contains(hits, m => m.StartsWith("literal White on Border.Background"));
        Assert.Contains(hits, m => m.StartsWith("literal Black in <SolidColorBrush>"));
    }

    [Fact]
    public void Hex_flags_colors_built_in_code()
    {
        var code = @"
using Microsoft.UI;
partial class MainPage : Page
{
    void Paint()
    {
        Title.Foreground = new SolidColorBrush(Colors.White);
        var c = Color.FromArgb(255, 122, 103, 248);
        var d = ColorHelper.FromArgb(255, 1, 2, 3);
        var none = Colors.Transparent;
        var fine = new SolidColorBrush((Color)Resources[""PrimaryColor""]);
    }
}";
        var hits = LintCs(code, "Painter.cs").Where(f => f.Rule == Rules.Hex).Select(f => f.Message).ToList();
        Assert.Equal(new[] { "Colors.White in code", "Color.FromArgb(...) in code", "ColorHelper.FromArgb(...) in code" }, hits);
    }

    // ------------------------------------------------------------ BINDVALUE (first correctness rule)

    [Fact]
    public void BindValue_flags_state_dot_value_in_mvux_projects_only()
    {
        var xaml = $@"<Page {Ns}>
  <TextBox Text=""{{Binding Name.Value, Mode=TwoWay}}"" />
  <TextBlock Text=""{{Binding Path=Profile.Address.Value}}"" />
  <TextBlock Text=""{{Binding Name, Mode=TwoWay}}"" />
  <TextBlock Text=""{{Binding Value}}"" />
  <ListView ItemsSource=""{{Binding Items}}""><ListView.ItemTemplate><DataTemplate><TextBlock Text=""{{Binding Pair.Value}}""/></DataTemplate></ListView.ItemTemplate></ListView>
</Page>";
        var mvux = LintXaml(xaml, mvux: true).Where(f => f.Rule == Rules.BindValue).ToList();
        Assert.Equal(2, mvux.Count);
        Assert.Contains(mvux, f => f.Line == 2 && f.Message.Contains("Name.Value") && f.Message.Contains("bind Name itself"));
        Assert.Contains(mvux, f => f.Line == 3 && f.Message.Contains("Profile.Address.Value"));

        Assert.Empty(LintXaml(xaml, mvux: false).Where(f => f.Rule == Rules.BindValue));
    }

    [Fact]
    public void BindValue_is_in_the_recommended_profile_and_gated_on_mvux()
    {
        var options = new LintOptions();
        Assert.Equal(Severity.Warning, Rules.Resolve(Rules.BindValue, options, new ProjectContext { HasMvux = true }));
        Assert.Equal(Severity.None, Rules.Resolve(Rules.BindValue, options, new ProjectContext { HasMvux = false }));
    }

    // ------------------------------------------------------------ CODEBEHIND signature matches the spec

    [Fact]
    public void CodeBehind_requires_an_args_typed_second_parameter()
    {
        var code = @"partial class P : Page {
  void A(object sender, RoutedEventArgs e) { }
  void B(object s, TappedRoutedEventArgs args) { }
  void C(object sender, string name) { }
  void D(object sender) { }
  void E(object sender, RoutedEventArgs e, int extra) { }
}";
        var hits = LintCs(code).Where(f => f.Rule == Rules.CodeBehind).Select(f => f.Message).ToList();
        Assert.Equal(new[] { "handler A", "handler B" }, hits);
    }

    // ------------------------------------------------------------ suppression as a metric

    [Fact]
    public void Suppressed_hits_are_counted_not_reported()
    {
        Write("App.csproj", "<Project Sdk=\"Uno.Sdk\"><PropertyGroup><UnoFeatures>Toolkit</UnoFeatures></PropertyGroup></Project>");
        // The allow comment reaches three non-blank lines, so the second Border sits further down.
        Write("MainPage.xaml", "<Page xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\">\n<!-- uno-lint: allow hex - chart series -->\n<Border Background=\"#FF0000\"/>\n<TextBlock/>\n<TextBlock/>\n<TextBlock/>\n<Border Background=\"#00FF00\"/>\n</Page>");

        var result = new Linter(new LintOptions()).Run(_root);
        Assert.Single(result.Findings);
        Assert.Single(result.Suppressed);
        Assert.Equal(1, result.SuppressedCount(Rules.Hex));
        Assert.Equal(0, result.SuppressedCount(Rules.Card));
    }

    [Fact]
    public void Suppressed_hits_of_disabled_rules_are_not_counted()
    {
        Write("App.csproj", "<Project/>");
        Write("MainPage.xaml.cs", "partial class P : Page {\n// uno-lint: allow codebehind - glue\nvoid H(object sender, EventArgs e) { } }");

        var result = new Linter(new LintOptions { Profile = Profile.Recommended }).Run(_root);
        Assert.Empty(result.Findings);
        Assert.Empty(result.Suppressed);
    }

    // ------------------------------------------------------------ TOKENTHEME ships at suggestion

    [Fact]
    public void TokenTheme_defaults_to_suggestion_and_does_not_gate()
    {
        Write("App.csproj", "<Project/>");
        Write("App.xaml", "<ResourceDictionary xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"><SolidColorBrush x:Key=\"StatusBrush\" Color=\"#18D96A\"/></ResourceDictionary>");

        var result = new Linter(new LintOptions()).Run(_root);
        var hit = Assert.Single(result.Findings);
        Assert.Equal(Rules.TokenTheme, hit.Rule);
        Assert.Equal(Severity.Suggestion, hit.Severity);
        Assert.False(result.HasGatingFindings);

        Write(".editorconfig", "root = true\n[*]\ndotnet_diagnostic.UNOL002.severity = warning\n");
        var options = new LintOptions();
        EditorConfig.Apply(_root, options, profileExplicit: false);
        Assert.True(new Linter(options).Run(_root).HasGatingFindings);
    }

    // ------------------------------------------------------------ theme-aware fix text

    [Fact]
    public void Hex_fix_text_depends_on_whether_uno_themes_is_referenced()
    {
        var themed = Rules.FixFor(Rules.Hex, new ProjectContext { HasUnoThemes = true });
        var fluent = Rules.FixFor(Rules.Hex, new ProjectContext { HasUnoThemes = false });
        Assert.Contains("Uno.Themes", themed);
        Assert.Contains("SurfaceBrush", themed);
        Assert.DoesNotContain("Uno.Themes", fluent);
        Assert.DoesNotContain("SurfaceBrush", fluent);
        Assert.Contains("TextFillColorPrimaryBrush", fluent);
    }

    [Theory]
    [InlineData("<UnoFeatures>Material;Toolkit</UnoFeatures>", true, true, false)]
    [InlineData("<UnoFeatures>SimpleTheme;Mvux</UnoFeatures>", true, false, true)]
    [InlineData("<UnoFeatures>MaterialToolkit</UnoFeatures>", true, true, false)]
    [InlineData("<UnoFeatures>Hosting</UnoFeatures>", false, false, false)]
    [InlineData("<PackageReference Include=\"Uno.Material.WinUI\" /><PackageReference Include=\"Uno.Extensions.Reactive.WinUI\" />", true, false, true)]
    public void Context_detects_themes_toolkit_and_mvux(string projectXml, bool themes, bool toolkit, bool mvux)
    {
        var ctx = ProjectContext.Detect(new[] { $"<Project>{projectXml}</Project>" }, Array.Empty<string>());
        Assert.Equal(themes, ctx.HasUnoThemes);
        Assert.Equal(toolkit, ctx.HasToolkit);
        Assert.Equal(mvux, ctx.HasMvux);
    }

    [Fact]
    public void Context_falls_back_to_xaml_namespaces()
    {
        var ctx = ProjectContext.Detect(Array.Empty<string>(), new[] { "<Page xmlns:um=\"using:Uno.Material\" xmlns:mvux=\"using:Uno.Extensions.Reactive.UI\"/>" });
        Assert.True(ctx.HasUnoThemes);
        Assert.True(ctx.HasMvux);
        Assert.False(ctx.HasToolkit);
    }

    [Fact]
    public void Findings_carry_the_project_specific_fix()
    {
        Write("App.csproj", "<Project Sdk=\"Uno.Sdk\"><PropertyGroup><UnoFeatures>Hosting</UnoFeatures></PropertyGroup></Project>");
        Write("MainPage.xaml", "<Page xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><Border Background=\"#FF0000\"/></Page>");

        var hit = Assert.Single(new Linter(new LintOptions()).Run(_root).Findings);
        Assert.Contains("TextFillColorPrimaryBrush", hit.Fix);
    }

    // ------------------------------------------------------------ hook-mode context cache

    [Fact]
    public void Single_file_context_is_cached_and_bin_obj_are_never_walked()
    {
        Write("App.csproj", "<Project Sdk=\"Uno.Sdk\"><PropertyGroup><UnoFeatures>Hosting</UnoFeatures></PropertyGroup></Project>");
        Write("Views/Main.xaml", "<Page xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:utu=\"using:Uno.Toolkit.UI\"><Border CornerRadius=\"8\" Background=\"White\"><Grid/></Border></Page>");
        Write("obj/Debug/Generated.xaml", "<Page xmlns:mvux=\"using:Uno.Extensions.Reactive.UI\"/>");

        var first = new Linter(new LintOptions { Profile = Profile.Strict }).RunSingleFile(Path.Combine(_root, "Views", "Main.xaml"));
        Assert.True(first.Context.HasToolkit, "Toolkit detected from the XAML namespace");
        Assert.False(first.Context.HasMvux, "obj/ is not walked");
        Assert.Single(first.Findings.Where(f => f.Rule == Rules.Card));

        var second = new Linter(new LintOptions { Profile = Profile.Strict }).RunSingleFile(Path.Combine(_root, "Views", "Main.xaml"));
        Assert.True(second.Context.HasToolkit);
    }
}
