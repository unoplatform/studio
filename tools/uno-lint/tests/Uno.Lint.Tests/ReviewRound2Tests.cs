using Uno.Lint;
using Xunit;

namespace Uno.Lint.Tests;

/// <summary>Regression tests for the second PR review round: directory-mode vs hook-mode disagreements and rule edge cases.</summary>
public class ReviewRound2Tests : IDisposable
{
    private const string Ns = "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" xmlns:utu=\"using:Uno.Toolkit.UI\"";
    private const string Page = "<Page xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\">";

    private readonly string _root = Path.Combine(Path.GetTempPath(), "uno-lint-tests", Guid.NewGuid().ToString("N"));

    public ReviewRound2Tests()
    {
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch { }
    }

    private string Write(string relative, string content)
    {
        var path = Path.Combine(_root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
        return path;
    }

    private static LintResult RunWithConfig(string pathOrDir, Profile profile = Profile.Strict)
    {
        var options = new LintOptions { Profile = profile };
        var configRoot = Directory.Exists(pathOrDir) ? pathOrDir : Linter.FindProjectRoot(Path.GetDirectoryName(pathOrDir)!);
        EditorConfig.Apply(configRoot, options, profileExplicit: true);
        return new Linter(options).Run(pathOrDir);
    }

    private static List<Finding> LintXaml(string xaml, string fileName = "MainPage.xaml")
    {
        return XamlTestHost.Lint(xaml, fileName, new ProjectContext { HasToolkit = true }, new LintOptions { Profile = Profile.Strict }, includeSuppressed: true);
    }

    private static List<Finding> LintCs(string code, string fileName = "MainPage.xaml.cs")
    {
        var ctx = new ProjectContext { HasToolkit = true };
        var options = new LintOptions { Profile = Profile.Strict };
        return CSharpAnalyzer.Analyze(new CSharpFile(fileName, code, null), ctx, r => Rules.Resolve(r, options, ctx) != Severity.None).Where(f => !f.Suppressed).ToList();
    }

    // ------------------------------------------------------------ blocking: nested .editorconfig in directory mode

    [Fact]
    public void Nested_editorconfig_applies_in_directory_mode_and_hook_mode_alike()
    {
        Write("App.csproj", "<Project/>");
        Write("App/.editorconfig", "[*.xaml]\ndotnet_diagnostic.UNOL001.severity = none\n");
        var page = Write("App/MainPage.xaml", Page + "<Border Background=\"#FF0000\"/></Page>");
        Write("Other/OtherPage.xaml", Page + "<Border Background=\"#FF0000\"/></Page>");

        var directory = RunWithConfig(_root);
        var single = RunWithConfig(page);

        Assert.DoesNotContain(directory.Findings, f => f.Rule == Rules.Hex && f.File.EndsWith("MainPage.xaml"));
        Assert.Contains(directory.Findings, f => f.Rule == Rules.Hex && f.File.EndsWith("OtherPage.xaml"));
        Assert.Empty(single.Findings);
    }

    // ------------------------------------------------------------ blocking: Directory.Build.props above the csproj

    [Fact]
    public void Directory_build_props_above_the_project_is_read_in_every_mode()
    {
        Write("Directory.Build.props", "<Project><PropertyGroup><UnoFeatures>Toolkit;Material</UnoFeatures></PropertyGroup></Project>");
        Write("src/App/App.csproj", "<Project Sdk=\"Uno.Sdk\"/>");
        var page = Write("src/App/MainPage.xaml", Page + "<Border CornerRadius=\"8\" Background=\"White\"><Grid/></Border></Page>");

        foreach (var target in new[] { _root, Path.Combine(_root, "src", "App"), page })
        {
            var result = RunWithConfig(target);
            Assert.True(result.Context.HasToolkit, target);
            Assert.True(result.Context.HasUnoThemes, target);
            Assert.Single(result.Findings, f => f.Rule == Rules.Card);
            Assert.Contains("PrimaryBrush", result.Findings.Single(f => f.Rule == Rules.Hex).Fix);
        }
    }

    // ------------------------------------------------------------ bug: HEX/TOKENTHEME fold keyed by line

    [Fact]
    public void Single_themed_literal_brush_is_one_gating_hex_finding_regardless_of_formatting()
    {
        var oneLine = LintXaml($"<ResourceDictionary {Ns}><SolidColorBrush x:Key=\"StatusBrush\" Color=\"#18D96A\"/></ResourceDictionary>", "App.xaml");
        var twoLines = LintXaml($"<ResourceDictionary {Ns}>\n<SolidColorBrush x:Key=\"StatusBrush\"\n    Color=\"#18D96A\"/>\n</ResourceDictionary>", "App.xaml");

        foreach (var findings in new[] { oneLine, twoLines })
        {
            var hit = Assert.Single(findings);
            Assert.Equal(Rules.Hex, hit.Rule);
            Assert.Contains("StatusBrush is also defined once for both themes", hit.Message);
        }
    }

    [Fact]
    public void Allow_hex_suppresses_the_folded_finding()
    {
        var xaml = $"<ResourceDictionary {Ns}>\n<!-- uno-lint: allow hex - status colour, fixed by design -->\n<SolidColorBrush x:Key=\"StatusBrush\" Color=\"#18D96A\"/>\n</ResourceDictionary>";
        var hit = Assert.Single(LintXaml(xaml, "App.xaml"));
        Assert.True(hit.Suppressed);
    }

    [Fact]
    public void TokenTheme_takes_over_when_hex_is_off_for_the_file()
    {
        // In a palette file HEX does not run, so a single-themed literal brush is still a TOKENTHEME suggestion.
        var hit = Assert.Single(LintXaml($"<ResourceDictionary {Ns}><SolidColorBrush x:Key=\"StatusBrush\" Color=\"#18D96A\"/></ResourceDictionary>", "Colors.xaml"));
        Assert.Equal(Rules.TokenTheme, hit.Rule);
    }

    // ------------------------------------------------------------ bug: unkeyed children inside ThemeDictionaries

    [Fact]
    public void Children_of_keyed_resources_inside_theme_dictionaries_are_exempt_from_hex()
    {
        var xaml = $@"<ResourceDictionary {Ns}>
  <ResourceDictionary.ThemeDictionaries>
    <ResourceDictionary x:Key=""Light"">
      <LinearGradientBrush x:Key=""HeroBrush""><GradientStop Color=""#FF0000"" Offset=""0""/></LinearGradientBrush>
      <SolidColorBrush x:Key=""InkBrush""><SolidColorBrush.Color>#222222</SolidColorBrush.Color></SolidColorBrush>
      <Style x:Key=""Pill"" TargetType=""Border""><Setter Property=""Background"" Value=""#333333""/></Style>
      <SolidColorBrush Color=""#444444""/>
    </ResourceDictionary>
  </ResourceDictionary.ThemeDictionaries>
</ResourceDictionary>";
        // Only the unkeyed brush at the dictionary level is a literal with no token behind it.
        var hit = Assert.Single(LintXaml(xaml, "App.xaml"), f => f.Rule == Rules.Hex);
        Assert.Contains("#444444", hit.Message);
    }

    // ------------------------------------------------------------ bug: App.xaml.cs lifecycle handlers

    [Fact]
    public void App_lifecycle_handlers_are_not_code_behind_findings()
    {
        var app = @"public sealed partial class App : Application {
  protected override void OnLaunched(LaunchActivatedEventArgs args) { }
  private void OnSuspending(object sender, SuspendingEventArgs e) { }
  private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e) { }
}";
        Assert.DoesNotContain(LintCs(app, "App.xaml.cs"), f => f.Rule == Rules.CodeBehind);
        Assert.DoesNotContain(LintCs(app, "Shell/MyApp.xaml.cs"), f => f.Rule == Rules.CodeBehind);

        var page = "public sealed partial class MainPage : Page { private void OnSuspending(object sender, SuspendingEventArgs e) { } }";
        Assert.Single(LintCs(page), f => f.Rule == Rules.CodeBehind);
    }

    // ------------------------------------------------------------ minor: preamble severities

    [Fact]
    public void Preamble_severity_keys_are_ignored_and_reported()
    {
        var set = new EditorConfigSet();
        set.AddText(new[] { "root = true", "dotnet_diagnostic.UNOL001.severity = none", "[*.xaml]", "dotnet_diagnostic.UNOL002.severity = none" }, _root);
        Assert.False(set.TryGetSeverity("UNOL001", Path.Combine(_root, "Main.xaml"), out _));
        Assert.True(set.TryGetSeverity("UNOL002", Path.Combine(_root, "Main.xaml"), out _));
        Assert.Equal(new[] { "UNOL001" }, set.IgnoredPreambleKeys);
    }

    // ------------------------------------------------------------ minor: suppression crosses element boundaries

    [Fact]
    public void Suppression_stops_at_the_previous_element()
    {
        var xaml = $@"<Page {Ns}>
  <Page.Resources>
    <!-- uno-lint: allow hex - chart series -->
    <SolidColorBrush x:Key=""SeriesInvariantBrush"" Color=""#FF0000""/>
  </Page.Resources>
  <Border Background=""#00FF00""/>
</Page>";
        var all = LintXaml(xaml).Where(f => f.Rule == Rules.Hex).ToList();
        Assert.Equal(2, all.Count);
        Assert.True(all.Single(f => f.Line == 4).Suppressed);
        Assert.False(all.Single(f => f.Line == 6).Suppressed);
    }

    [Fact]
    public void Suppression_still_reaches_through_consecutive_comment_lines()
    {
        var code = @"partial class P : Page {
  // The transport bar binds to a MediaPlayerElement that has no command surface.
  // uno-lint: allow codebehind - media transport glue
  // See issue #123 for the upstream request.
  void OnPositionChanged(object sender, RoutedEventArgs e) { }
  void OnOther(object sender, RoutedEventArgs e) { }
}";
        var hits = LintCs(code).Where(f => f.Rule == Rules.CodeBehind).Select(f => f.Message).ToList();
        Assert.Equal(new[] { "handler OnOther" }, hits);
    }

    // ------------------------------------------------------------ minor: OVERLAY panel suffix, C# palette exemption

    [Fact]
    public void Overlay_does_not_treat_plain_panels_as_overlays()
    {
        var code = "partial class P : Page { void X() { RootPanel.Visibility = Visibility.Collapsed; FilterDrawer.Visibility = Visibility.Visible; } }";
        var hits = LintCs(code);
        Assert.Single(hits, f => f.Rule == Rules.Overlay && f.Message.StartsWith("FilterDrawer"));
        Assert.Single(hits, f => f.Rule == Rules.CodeBehind && f.Message.StartsWith("RootPanel"));
    }

    [Theory]
    [InlineData("Palette.cs", 0)]
    [InlineData("Theme/AppColors.cs", 0)]
    [InlineData("DesignTokens.cs", 0)]
    [InlineData("ColorPickerViewModel.cs", 1)]
    public void Color_literals_in_code_follow_the_palette_file_exemption(string fileName, int expected)
    {
        var code = "static class T { public static readonly Color Brand = Color.FromArgb(255, 1, 2, 3); }";
        Assert.Equal(expected, LintCs(code, fileName).Count(f => f.Rule == Rules.Hex));
    }

    // ------------------------------------------------------------ nit: path mode input, dev-path skip in single-file mode

    [Fact]
    public void Non_source_files_are_rejected_not_parsed_as_csharp()
    {
        var notes = Write("notes.txt", "void H(object sender, EventArgs e) { }");
        Assert.False(FileKinds.IsSourceFile(notes));
        Assert.Throws<ArgumentException>(() => new Linter(new LintOptions()).Run(notes));
    }

    [Fact]
    public void Dev_path_skip_applies_to_single_files_too()
    {
        Write("App.csproj", "<Project/>");
        var probe = Write("Tests/Probe.xaml", Page + "<Border Background=\"#FF0000\"/></Page>");

        var skipped = new Linter(new LintOptions()).Run(probe);
        Assert.Equal(0, skipped.FileCount);
        Assert.Equal(1, skipped.SkippedDevFiles);
        Assert.Empty(skipped.Findings);

        var included = new Linter(new LintOptions { IncludeDev = true }).Run(probe);
        var hit = Assert.Single(included.Findings);
        Assert.Equal(Path.Combine("Tests", "Probe.xaml"), hit.File);
    }
}
