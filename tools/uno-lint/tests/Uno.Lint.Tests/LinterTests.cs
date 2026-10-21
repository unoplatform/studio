using Xunit;
using static Uno.Lint.Tests.TestHost;

namespace Uno.Lint.Tests;

/// <summary>The file-system host: which files are linted, how the project context is found, and single-file runs.</summary>
public class LinterTests : IDisposable
{
    private const string ToolkitProject = "<Project Sdk=\"Uno.Sdk\"><PropertyGroup><UnoFeatures>Toolkit</UnoFeatures></PropertyGroup></Project>";

    private readonly TempProject _project = new();

    public void Dispose() => _project.Dispose();

    // ------------------------------------------------------------ project context

    [Fact]
    public void Detects_toolkit_from_UnoFeatures_and_enables_card_rule()
    {
        _project.Write("App.csproj", "<Project Sdk=\"Uno.Sdk\"><PropertyGroup><UnoFeatures>Material;Toolkit;Mvux</UnoFeatures></PropertyGroup></Project>");
        _project.Write("MainPage.xaml", $"{Page}<Border CornerRadius=\"8\" Background=\"Red\"/></Page>");

        var strict = _project.Lint(options: new LintOptions { Profile = Profile.Strict });
        Assert.True(strict.Context.HasToolkit);
        Assert.Single(strict.Findings, f => f.Rule == Rules.Card);
        Assert.DoesNotContain(_project.Lint().Findings, f => f.Rule == Rules.Card);
    }

    [Fact]
    public void Without_toolkit_card_rule_stays_off_even_in_strict()
    {
        _project.Write("App.csproj", "<Project Sdk=\"Uno.Sdk\"><PropertyGroup><UnoFeatures>Mvux</UnoFeatures></PropertyGroup></Project>");
        _project.Write("MainPage.xaml", $"{Page}<Border CornerRadius=\"8\" Background=\"#FF0000\"/></Page>");

        var result = _project.Lint(options: new LintOptions { Profile = Profile.Strict });
        Assert.False(result.Context.HasToolkit);
        Assert.DoesNotContain(result.Findings, f => f.Rule == Rules.Card);
        Assert.Single(result.Findings, f => f.Rule == Rules.Hex);

        Assert.Single(_project.Lint(options: new LintOptions { Profile = Profile.Strict, AssumeToolkit = true }).Findings, f => f.Rule == Rules.Card);
    }

    [Theory]
    [InlineData("<UnoFeatures>Material;Toolkit</UnoFeatures>", true, true, false)]
    [InlineData("<UnoFeatures>SimpleTheme;Mvux</UnoFeatures>", true, false, true)]
    [InlineData("<UnoFeatures>MaterialToolkit</UnoFeatures>", true, true, false)]
    [InlineData("<UnoFeatures>Hosting</UnoFeatures>", false, false, false)]
    [InlineData("<PackageReference Include=\"Uno.Material.WinUI\" /><PackageReference Include=\"Uno.Extensions.Reactive.WinUI\" />", true, false, true)]
    public void Context_detects_themes_toolkit_and_mvux(string projectXml, bool themes, bool toolkit, bool mvux)
    {
        var context = ProjectContext.Detect([$"<Project>{projectXml}</Project>"], []);
        Assert.Equal(themes, context.HasUnoThemes);
        Assert.Equal(toolkit, context.HasToolkit);
        Assert.Equal(mvux, context.HasMvux);
    }

    [Fact]
    public void Context_falls_back_to_xaml_namespaces()
    {
        var context = ProjectContext.Detect([], ["<Page xmlns:um=\"using:Uno.Material\" xmlns:mvux=\"using:Uno.Extensions.Reactive.UI\"/>"]);
        Assert.True(context.HasUnoThemes);
        Assert.True(context.HasMvux);
        Assert.False(context.HasToolkit);
    }

    [Fact]
    public void Directory_build_props_above_the_project_is_read_in_every_mode()
    {
        _project.Write("Directory.Build.props", "<Project><PropertyGroup><UnoFeatures>Toolkit;Material</UnoFeatures></PropertyGroup></Project>");
        _project.Write("src/App/App.csproj", "<Project Sdk=\"Uno.Sdk\"/>");
        _project.Write("src/App/MainPage.xaml", $"{Page}<Border CornerRadius=\"8\" Background=\"White\"><Grid/></Border></Page>");

        foreach (var target in new[] { null, "src/App", "src/App/MainPage.xaml" })
        {
            var result = _project.Lint(target, new LintOptions { Profile = Profile.Strict });
            Assert.True(result.Context.HasToolkit, target);
            Assert.True(result.Context.HasUnoThemes, target);
            Assert.Single(result.Findings, f => f.Rule == Rules.Card);
            Assert.Contains("PrimaryBrush", result.Findings.Single(f => f.Rule == Rules.Hex).Fix);
        }
    }

    [Fact]
    public void Findings_carry_the_project_specific_fix()
    {
        _project.Write("App.csproj", "<Project Sdk=\"Uno.Sdk\"><PropertyGroup><UnoFeatures>Hosting</UnoFeatures></PropertyGroup></Project>");
        _project.Write("MainPage.xaml", $"{Page}<Border Background=\"#FF0000\"/></Page>");
        Assert.Contains("TextFillColorPrimaryBrush", Assert.Single(_project.Lint().Findings).Fix);
    }

    // ------------------------------------------------------------ which files

    [Fact]
    public void Skips_dev_paths_bin_obj_and_generated_files()
    {
        _project.Write("App.csproj", "<Project/>");
        _project.Write("Tests/Sample.xaml", $"{Page}<Border Background=\"#FF0000\"/></Page>");
        _project.Write("obj/MainPage.g.cs", "class X { void H(object sender, EventArgs e) { } }");
        _project.Write("MainPage.xaml.cs", "partial class MainPage { void H(object sender, EventArgs e) { } }");

        var result = _project.Lint(options: new LintOptions { Profile = Profile.Strict });
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
        Assert.Equal(isDev, ProjectLocator.IsDevPath(relative));
    }

    [Fact]
    public void Dev_path_skip_applies_to_single_files_too()
    {
        _project.Write("App.csproj", "<Project/>");
        _project.Write("Tests/Probe.xaml", $"{Page}<Border Background=\"#FF0000\"/></Page>");

        var skipped = _project.Lint("Tests/Probe.xaml");
        Assert.Equal(0, skipped.FileCount);
        Assert.Equal(1, skipped.SkippedDevFiles);
        Assert.Empty(skipped.Findings);

        var hit = Assert.Single(_project.Lint("Tests/Probe.xaml", new LintOptions { IncludeDev = true }).Findings);
        Assert.Equal(Path.Combine("Tests", "Probe.xaml"), hit.File);
    }

    [Fact]
    public void Generated_files_are_skipped_in_single_file_runs_too()
    {
        _project.Write("App.csproj", "<Project/>");
        _project.Write("Views/MainPage.g.cs", "partial class MainPage { void H(object sender, EventArgs e) { } }");

        Assert.Equal(0, _project.Lint("Views/MainPage.g.cs").FileCount);
    }

    [Fact]
    public void Non_source_files_are_rejected_not_parsed_as_csharp()
    {
        _project.Write("notes.txt", "void H(object sender, EventArgs e) { }");
        Assert.False(FileKinds.IsSourceFile("notes.txt"));
        Assert.Throws<ArgumentException>(() => _project.Lint("notes.txt"));
    }

    [Fact]
    public void Findings_are_in_file_and_line_order()
    {
        _project.Write("App.csproj", "<Project/>");
        _project.Write("B.xaml", $"{Page}\n<Border Background=\"#00FF00\"/>\n<Border Background=\"#FF0000\"/>\n</Page>");
        _project.Write("A.xaml", $"{Page}\n<Border Background=\"#0000FF\"/>\n</Page>");

        Assert.Equal(["A.xaml:2", "B.xaml:2", "B.xaml:3"], _project.Lint().Findings.Select(f => $"{f.File}:{f.Line}"));
    }

    [Fact]
    public void A_crafted_symlink_loop_does_not_hang_the_walk()
    {
        _project.Write("App.csproj", "<Project/>");
        _project.Write("Views/MainPage.xaml", $"{Page}<Border Background=\"#FF0000\"/></Page>");
        try
        {
            Directory.CreateSymbolicLink(_project.Path("Views/Up"), _project.Root);
            Directory.CreateSymbolicLink(_project.Path("Views/Self"), _project.Path("Views"));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return; // creating links needs Developer Mode or elevation on Windows; Linux CI runs this
        }

        var result = _project.Lint();
        Assert.Equal(1, result.FileCount);
        Assert.Single(result.Findings);
    }

    // ------------------------------------------------------------ single files

    [Fact]
    public void Single_file_mode_detects_context_from_nearest_csproj()
    {
        _project.Write("App.csproj", ToolkitProject);
        _project.Write("Views/Detail.xaml", $"{Page}<Border CornerRadius=\"8\" Background=\"Red\"/></Page>");

        var result = _project.Lint("Views/Detail.xaml", new LintOptions { Profile = Profile.Strict });
        Assert.True(result.Context.HasToolkit);
        Assert.Single(result.Findings, f => f.Rule == Rules.Card);
    }

    [Fact]
    public void A_file_outside_any_project_gets_an_empty_context_and_its_ancestors_editorconfig()
    {
        // No .csproj anywhere above: the folder is not walked for project files or XAML namespaces.
        _project.Write(".editorconfig", "root = true\n[*.xaml]\ndotnet_diagnostic.UNOL003.severity = none\n");
        _project.Write("big/deep/Other.xaml", $"<Page {Ns}><utu:Card/></Page>");
        _project.Write("Scratch.xaml", $"{Page}<Border Background=\"#FF0000\"/><Path Data=\"M0,0 L1,1\"/></Page>");
        if (ProjectLocator.FindProjectRoot(_project.Root) is { } above)
        {
            Assert.Fail($"the temp folder sits inside a project ({above}); this test needs a folder outside any project");
        }


        var result = _project.Lint("Scratch.xaml", new LintOptions { Profile = Profile.Strict });
        Assert.False(result.Context.HasToolkit, "the sibling folder's Toolkit namespace is not read");
        Assert.Equal(["HEX"], result.Findings.Select(f => f.Rule.Name));
    }

    [Fact]
    public async Task Single_file_context_is_cached_per_project_and_bin_obj_are_never_walked()
    {
        // The cache folder is outside the project: a folder inside it would change the key's entry count.
        var cache = _project.Root + ".cache";
        try
        {
            _project.Write("App.csproj", "<Project Sdk=\"Uno.Sdk\"><PropertyGroup><UnoFeatures>Hosting</UnoFeatures></PropertyGroup></Project>");
            _project.Write("Views/Main.xaml", $"{Page.Replace(">", " xmlns:utu=\"using:Uno.Toolkit.UI\">")}<Border CornerRadius=\"8\" Background=\"White\"><Grid/></Border></Page>");
            _project.Write("obj/Debug/Generated.xaml", "<Page xmlns:mvux=\"using:Uno.Extensions.Reactive.UI\"/>");
            var linter = new Linter(new LintOptions { Profile = Profile.Strict, ContextCacheDirectory = cache }, Engine);
            var file = _project.Path("Views/Main.xaml");

            var first = await linter.RunAsync(file);
            Assert.True(first.Context.HasToolkit, "Toolkit detected from the XAML namespace");
            Assert.False(first.Context.HasMvux, "obj/ is not walked");
            var entry = Assert.Single(Directory.GetFiles(cache));
            Assert.Equal("T---", File.ReadAllText(entry));

            // The cached entry is what the next call reads: edit it and the context follows.
            File.WriteAllText(entry, "T-M-");
            Assert.True((await linter.RunAsync(file)).Context.HasMvux);

            // --no-cache detects again.
            Assert.False((await new Linter(new LintOptions { UseContextCache = false }, Engine).RunAsync(file)).Context.HasMvux);
        }
        finally
        {
            if (Directory.Exists(cache))
            {
                Directory.Delete(cache, recursive: true);
            }
        }
    }

    [Fact]
    public void Workaround_markers_are_counted()
    {
        _project.Write("App.csproj", "<Project/>");
        _project.Write("MainPage.xaml.cs", "partial class P { // WORKAROUND(#1) a\n // WORKAROUND(#2) b WORKAROUND(\n }");
        Assert.Equal(3, _project.Lint().Info.WorkaroundMarkers);
    }

    // ------------------------------------------------------------ folder and file runs agree

    [Fact]
    public void A_subfolder_run_uses_the_project_above_it()
    {
        _project.Write("App/App.csproj", "<Project Sdk=\"Uno.Sdk\"><PropertyGroup><UnoFeatures>Toolkit;MVUX;Material</UnoFeatures></PropertyGroup></Project>");
        _project.Write("App/Views/MainPage.xaml", $"{Page}<Border CornerRadius=\"8\" Background=\"White\"><Grid/></Border><TextBlock Text=\"{{Binding Name.Value}}\"/></Page>");

        foreach (var target in new[] { null, "App", "App/Views", "App/Views/MainPage.xaml" })
        {
            var result = _project.Lint(target, new LintOptions { Profile = Profile.Strict });
            Assert.True(result.Context.HasToolkit, target);
            Assert.Equal(["BINDVALUE", "CARD", "HEX"], result.Findings.Select(f => f.Rule.Name).Order());
            Assert.Contains("PrimaryBrush", result.Findings.Single(f => f.Rule == Rules.Hex).Fix);
        }
    }

    [Fact]
    public void A_project_folder_named_like_a_test_project_is_skipped_from_any_start()
    {
        _project.Write("p.sln", "");
        _project.Write("App.Tests/App.Tests.csproj", "<Project/>");
        _project.Write("App.Tests/Views/TestPage.xaml", $"{Page}<Border Background=\"#FF0000\"/></Page>");

        foreach (var target in new[] { null, "App.Tests", "App.Tests/Views/TestPage.xaml" })
        {
            var result = _project.Lint(target);
            Assert.Equal(0, result.FileCount);
            Assert.Equal(1, result.SkippedDevFiles);
        }
    }

    [Fact]
    public void A_dev_folder_above_the_project_counts_up_to_the_solution()
    {
        _project.Write("p.sln", "");
        _project.Write("tests/UnitTests/UnitTests.csproj", "<Project/>");
        _project.Write("tests/UnitTests/Page.xaml", $"{Page}<Border Background=\"#FF0000\"/></Page>");

        Assert.Equal(1, _project.Lint().SkippedDevFiles);
        Assert.Equal(1, _project.Lint("tests/UnitTests/Page.xaml").SkippedDevFiles);
    }
}
