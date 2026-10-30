using Xunit;
using static Uno.Lint.Tests.TestHost;

namespace Uno.Lint.Tests;

/// <summary>.editorconfig: sections, globs, nesting, the profile key and severity values, read as a Roslyn analyzer would.</summary>
public class EditorConfigTests : IDisposable
{
    private readonly TempProject _project = new();

    public void Dispose() => _project.Dispose();

    [Fact]
    public void Sections_apply_only_to_matching_files()
    {
        _project.Write(".editorconfig", "root = true\n[App/Tests/**]\ndotnet_diagnostic.UNOL001.severity = none\n[*.xaml.cs]\ndotnet_diagnostic.UNOL103.severity = suggestion\n");
        _project.Write("App.csproj", "<Project Sdk=\"Uno.Sdk\"><PropertyGroup><UnoFeatures>Toolkit</UnoFeatures></PropertyGroup></Project>");
        var page = $"{Page}<Border Background=\"#FF0000\"/></Page>";
        _project.Write("App/Views/Main.xaml", page);
        _project.Write("App/Tests/Probe.xaml", page);
        _project.Write("App/Views/Main.xaml.cs", "partial class Main { void H(object sender, EventArgs e) { } }");

        var result = _project.Lint(options: new LintOptions { Profile = Profile.Strict, IncludeDev = true });

        // The probe's HEX is off; the real page's HEX is not.
        var hit = Assert.Single(result.Findings, f => f.Rule == Rules.Hex);
        Assert.Equal(Path.Combine("App", "Views", "Main.xaml"), hit.File);

        // CODEBEHIND stays on but at suggestion severity, so it no longer gates.
        var codeBehind = Assert.Single(result.Findings, f => f.Rule == Rules.CodeBehind);
        Assert.Equal(Severity.Suggestion, codeBehind.Severity);
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
    public void Globs_follow_editorconfig_semantics(string glob, string relativeFile, bool matches)
    {
        var set = new EditorConfigSet();
        set.AddText([$"[{glob}]", "dotnet_diagnostic.UNOL001.severity = none"], _project.Root);
        Assert.Equal(matches, set.TryGetSeverity("UNOL001", _project.Path(relativeFile), out var severity));
        if (matches)
        {
            Assert.Equal(Severity.None, severity);
        }
    }

    [Fact]
    public void Preamble_severity_keys_are_ignored_and_reported()
    {
        var set = new EditorConfigSet();
        set.AddText(["root = true", "dotnet_diagnostic.UNOL001.severity = none", "[*.xaml]", "dotnet_diagnostic.UNOL002.severity = none"], _project.Root);
        Assert.False(set.TryGetSeverity("UNOL001", _project.Path("Main.xaml"), out _));
        Assert.True(set.TryGetSeverity("UNOL002", _project.Path("Main.xaml"), out _));
        Assert.Equal(["UNOL001"], set.IgnoredPreambleKeys);
        Assert.Equal(["UNOL002"], set.SectionedRuleIds());
    }

    [Fact]
    public void Default_restores_the_profile_severity_over_an_earlier_section()
    {
        _project.Write(".editorconfig", "root = true\n[*]\ndotnet_diagnostic.UNOL001.severity = none\ndotnet_diagnostic.UNOL101.severity = error\n[Views/*.xaml]\ndotnet_diagnostic.UNOL001.severity = default\ndotnet_diagnostic.UNOL101.severity = default\n");
        _project.Write("App.csproj", "<Project Sdk=\"Uno.Sdk\"><PropertyGroup><UnoFeatures>Toolkit</UnoFeatures></PropertyGroup></Project>");
        var card = $"{Page}<Border CornerRadius=\"8\" Background=\"#FF0000\"><Grid/></Border></Page>";
        _project.Write("Views/Main.xaml", card);
        _project.Write("Other.xaml", card);

        var recommended = _project.Lint();
        // Views: HEX back to warning; CARD back to its default, which is off under recommended. Other: HEX off, CARD error.
        Assert.Equal(["Views/Main.xaml HEX warning", "Other.xaml CARD error"],
            recommended.Findings.Select(f => $"{f.File.Replace('\\', '/')} {f.Rule.Name} {f.Severity.ToString().ToLowerInvariant()}").OrderByDescending(s => s));

        var strict = _project.Lint(options: new LintOptions { Profile = Profile.Strict });
        Assert.Contains(strict.Findings, f => f.File.EndsWith("Main.xaml") && f.Rule == Rules.Card && f.Severity == Severity.Warning);
    }

    [Fact]
    public void The_profile_comes_from_editorconfig_unless_given_explicitly()
    {
        _project.Write(".editorconfig", "root = true\n[*]\nuno_lint.profile = strict\ndotnet_diagnostic.UNOL001.severity = none\n");
        _project.Write("App.csproj", "<Project><ItemGroup><PackageReference Include=\"Uno.Toolkit.WinUI\" /></ItemGroup></Project>");
        _project.Write("MainPage.xaml", $"{Page}<Border CornerRadius=\"8\" Background=\"#FF0000\"/></Page>");

        var fromConfig = _project.Lint();
        Assert.Equal(Profile.Strict, fromConfig.Profile);
        Assert.DoesNotContain(fromConfig.Findings, f => f.Rule == Rules.Hex);
        Assert.Single(fromConfig.Findings, f => f.Rule == Rules.Card);

        Assert.Equal(Profile.Recommended, _project.Lint(options: new LintOptions { Profile = Profile.Recommended }).Profile);
    }

    [Fact]
    public void The_default_profile_is_recommended()
    {
        Assert.Null(new LintOptions().Profile);
        Assert.Equal(Profile.Recommended, new LintOptions().DefaultProfile);
    }

    [Fact]
    public void Nested_editorconfig_applies_in_directory_mode_and_single_file_mode_alike()
    {
        _project.Write("App.csproj", "<Project/>");
        _project.Write("App/.editorconfig", "[*.xaml]\ndotnet_diagnostic.UNOL001.severity = none\n");
        _project.Write("App/MainPage.xaml", $"{Page}<Border Background=\"#FF0000\"/></Page>");
        _project.Write("Other/OtherPage.xaml", $"{Page}<Border Background=\"#FF0000\"/></Page>");

        var directory = _project.Lint(options: new LintOptions { Profile = Profile.Strict });
        Assert.DoesNotContain(directory.Findings, f => f.File.EndsWith("MainPage.xaml"));
        Assert.Contains(directory.Findings, f => f.File.EndsWith("OtherPage.xaml"));

        Assert.Empty(_project.Lint("App/MainPage.xaml", new LintOptions { Profile = Profile.Strict }).Findings);
    }

    [Fact]
    public void TokenTheme_defaults_to_suggestion_and_gates_once_configured_as_warning()
    {
        _project.Write("App.csproj", "<Project/>");
        _project.Write("App.xaml", $"<ResourceDictionary {Ns}><SolidColorBrush x:Key=\"StatusBrush\" Color=\"{{StaticResource StatusColor}}\"/></ResourceDictionary>");

        var result = _project.Lint();
        var hit = Assert.Single(result.Findings);
        Assert.Equal(Rules.TokenTheme, hit.Rule);
        Assert.Equal(Severity.Suggestion, hit.Severity);
        Assert.False(result.HasGatingFindings);

        _project.Write(".editorconfig", "root = true\n[*]\ndotnet_diagnostic.UNOL002.severity = warning\n");
        Assert.True(_project.Lint().HasGatingFindings);
    }

    [Fact]
    public void A_nested_root_editorconfig_stops_the_chain_in_folder_runs_too()
    {
        _project.Write(".editorconfig", "root = true\n[*.xaml]\ndotnet_diagnostic.UNOL001.severity = none\n");
        _project.Write("App/.editorconfig", "root = true\n");
        _project.Write("App/App.csproj", "<Project/>");
        _project.Write("App/Views/MainPage.xaml", $"{Page}<Border Background=\"#FF0000\"/></Page>");

        Assert.Single(_project.Lint().Findings, f => f.Rule == Rules.Hex);
        Assert.Single(_project.Lint("App/Views/MainPage.xaml").Findings, f => f.Rule == Rules.Hex);
    }

    [Fact]
    public void A_nested_profile_applies_only_below_its_editorconfig()
    {
        _project.Write(".editorconfig", "root = true\n");
        _project.Write("Samples/.editorconfig", "[*]\nuno_lint.profile = strict\n");
        _project.Write("App.csproj", "<Project Sdk=\"Uno.Sdk\"><PropertyGroup><UnoFeatures>Toolkit</UnoFeatures></PropertyGroup></Project>");
        var card = $"{Page}<Border CornerRadius=\"8\" Background=\"{{ThemeResource SurfaceBrush}}\"><Grid/></Border></Page>";
        _project.Write("App/Views/MainPage.xaml", card);
        _project.Write("Samples/SamplePage.xaml", card);

        var result = _project.Lint();
        Assert.Equal(Profile.Recommended, result.Profile);
        Assert.Equal([Path.Combine("Samples", "SamplePage.xaml")], result.Findings.Where(f => f.Rule == Rules.Card).Select(f => f.File));
        Assert.Empty(_project.Lint("App/Views/MainPage.xaml").Findings);
        Assert.Equal(Profile.Strict, _project.Lint("Samples/SamplePage.xaml").Profile);
    }
}
