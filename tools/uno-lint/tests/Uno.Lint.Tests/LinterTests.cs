using Uno.Lint;
using Xunit;

namespace Uno.Lint.Tests;

public class LinterTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "uno-lint-tests", Guid.NewGuid().ToString("N"));

    public LinterTests()
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

    [Fact]
    public void Detects_toolkit_from_UnoFeatures_and_enables_card_rule()
    {
        Write("App.csproj", "<Project Sdk=\"Uno.Sdk\"><PropertyGroup><UnoFeatures>Material;Toolkit;Mvux</UnoFeatures></PropertyGroup></Project>");
        Write("MainPage.xaml", "<Page xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><Border CornerRadius=\"8\" Background=\"Red\"/></Page>");

        var strict = new Linter(new LintOptions { Profile = Profile.Strict }).Run(_root);
        Assert.True(strict.Context.HasToolkit);
        Assert.Single(strict.Findings.Where(f => f.Rule == Rules.Card));

        var recommended = new Linter(new LintOptions()).Run(_root);
        Assert.Empty(recommended.Findings.Where(f => f.Rule == Rules.Card));
    }

    [Fact]
    public void Without_toolkit_card_rule_stays_off_even_in_strict()
    {
        Write("App.csproj", "<Project Sdk=\"Uno.Sdk\"><PropertyGroup><UnoFeatures>Mvux</UnoFeatures></PropertyGroup></Project>");
        Write("MainPage.xaml", "<Page xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><Border CornerRadius=\"8\" Background=\"#FF0000\"/></Page>");

        var result = new Linter(new LintOptions { Profile = Profile.Strict }).Run(_root);
        Assert.False(result.Context.HasToolkit);
        Assert.Empty(result.Findings.Where(f => f.Rule == Rules.Card));
        Assert.Single(result.Findings.Where(f => f.Rule == Rules.Hex));
    }

    [Fact]
    public void Editorconfig_overrides_severity_and_profile()
    {
        Write(".editorconfig", "root = true\n[*]\nuno_lint.profile = strict\ndotnet_diagnostic.UNOL001.severity = none\n");
        Write("App.csproj", "<Project><ItemGroup><PackageReference Include=\"Uno.Toolkit.WinUI\" /></ItemGroup></Project>");
        Write("MainPage.xaml", "<Page xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><Border CornerRadius=\"8\" Background=\"#FF0000\"/></Page>");

        var options = new LintOptions();
        EditorConfig.Apply(_root, options, profileExplicit: false);
        Assert.Equal(Profile.Strict, options.Profile);

        var result = new Linter(options).Run(_root);
        Assert.Empty(result.Findings.Where(f => f.Rule == Rules.Hex));
        Assert.Single(result.Findings.Where(f => f.Rule == Rules.Card));
    }

    [Fact]
    public void Skips_dev_paths_bin_obj_and_generated_files()
    {
        Write("App.csproj", "<Project/>");
        Write("Tests/Sample.xaml", "<Page xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><Border Background=\"#FF0000\"/></Page>");
        Write("obj/MainPage.g.cs", "class X { void H(object sender, EventArgs e) { } }");
        Write("MainPage.xaml.cs", "partial class MainPage { void H(object sender, EventArgs e) { } }");

        var result = new Linter(new LintOptions { Profile = Profile.Strict }).Run(_root);
        Assert.Equal(1, result.FileCount);
        Assert.Equal(1, result.SkippedDevFiles);
        Assert.Single(result.Findings);
    }

    [Theory]
    [InlineData("Tests/Foo.xaml", true)]
    [InlineData("Dev/Scratch.xaml", true)]
    [InlineData("src/MyApp.Tests/MainPageTests.cs", true)]
    [InlineData("Content/Probes/Layout.xaml", true)]
    [InlineData("Views/ProbeResultsPage.xaml", false)]
    [InlineData("Views/HarnessPage.xaml", false)]
    [InlineData("TestApp/MainPage.xaml", false)]
    [InlineData("Content/TestPages/Foo.xaml", false)]
    [InlineData("Tests.xaml", false)]
    public void Dev_paths_match_whole_folder_names_only(string relative, bool isDev)
    {
        Assert.Equal(isDev, Linter.IsDevPath(relative));
    }

    [Fact]
    public void Skips_a_top_level_tests_folder_but_not_a_probe_named_page()
    {
        Write("App.csproj", "<Project/>");
        Write("Tests/Foo.xaml", "<Page xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><Border Background=\"#FF0000\"/></Page>");
        Write("Views/ProbeResultsPage.xaml", "<Page xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><Border Background=\"#FF0000\"/></Page>");

        var result = new Linter(new LintOptions()).Run(_root);
        Assert.Equal(1, result.SkippedDevFiles);
        var hit = Assert.Single(result.Findings);
        Assert.Equal(Path.Combine("Views", "ProbeResultsPage.xaml"), hit.File);
    }

    [Fact]
    public void Single_file_mode_detects_context_from_nearest_csproj()
    {
        Write("App.csproj", "<Project Sdk=\"Uno.Sdk\"><PropertyGroup><UnoFeatures>Toolkit</UnoFeatures></PropertyGroup></Project>");
        Write("Views/Detail.xaml", "<Page xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><Border CornerRadius=\"8\" Background=\"Red\"/></Page>");

        var result = new Linter(new LintOptions { Profile = Profile.Strict }).RunSingleFile(Path.Combine(_root, "Views", "Detail.xaml"));
        Assert.True(result.Context.HasToolkit);
        Assert.Single(result.Findings.Where(f => f.Rule == Rules.Card));
    }
}
