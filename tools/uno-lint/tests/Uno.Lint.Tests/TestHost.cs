using Uno.Lint.Xaml;

namespace Uno.Lint.Tests;

/// <summary>Helpers shared by every test class: one XAML engine, rule-level lint calls and throwaway project folders.</summary>
internal static class TestHost
{
    /// <summary>The namespaces a page declares: presentation, x:, utu: (Toolkit) and uen: (Extensions navigation).</summary>
    public const string Ns = """xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" xmlns:utu="using:Uno.Toolkit.UI" xmlns:uen="using:Uno.Extensions.Navigation.UI" """;

    /// <summary>A page start tag with only the presentation namespace.</summary>
    public const string Page = """<Page xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">""";

    /// <summary>One engine for the whole run, as the CLI does: building the parser pipelines costs more than a lint.</summary>
    public static readonly XamlLintEngine Engine = new();

    public static ProjectContext Context(bool toolkit = true, bool mvux = false, bool themes = false) => new(hasToolkit: toolkit, hasMvux: mvux, hasUnoThemes: themes);

    /// <summary>Lints one XAML document with every rule the profile and context enable; active findings unless asked otherwise.</summary>
    public static List<Finding> LintXaml(string xaml, string fileName = "MainPage.xaml", bool toolkit = true, bool mvux = false, Profile profile = Profile.Strict, bool includeSuppressed = false)
    {
        var context = Context(toolkit, mvux);
        return AnalyzeXaml(xaml, fileName, rule => Rules.Resolve(rule, profile, context) != Severity.None)
            .Findings.Where(f => includeSuppressed || !f.Suppressed).ToList();
    }

    public static XamlFileFindings AnalyzeXaml(string xaml, string fileName = "MainPage.xaml", Func<RuleDescriptor, bool>? isEnabled = null) =>
        Engine.AnalyzeAsync(fileName, xaml, isEnabled ?? (_ => true)).AsTask().GetAwaiter().GetResult();

    /// <summary>Lints one C# file with every rule the profile and context enable; active findings unless asked otherwise.</summary>
    public static List<Finding> LintCs(string code, string fileName = "MainPage.xaml.cs", string? companionXaml = null, bool toolkit = true, Profile profile = Profile.Strict, bool includeSuppressed = false)
    {
        var context = Context(toolkit);
        return CSharpAnalyzer.Analyze(new CSharpFile(fileName, code, companionXaml), context, rule => Rules.Resolve(rule, profile, context) != Severity.None)
            .Where(f => includeSuppressed || !f.Suppressed).ToList();
    }
}

/// <summary>A throwaway folder for tests that lint files on disk. The project context cache is bypassed.</summary>
internal sealed class TempProject : IDisposable
{
    public TempProject()
    {
        Root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "uno-lint-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Root);
    }

    public string Root { get; }

    public string Path(string relative) => System.IO.Path.Combine(Root, relative);

    /// <summary>Writes a file (creating its folders) and returns its full path.</summary>
    public string Write(string relative, string content)
    {
        var path = Path(relative);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
        return path;
    }

    /// <summary>Lints a file or folder under the root (the root itself by default).</summary>
    public LintResult Lint(string? relative = null, LintOptions? options = null) =>
        new Linter((options ?? new LintOptions()) with { UseContextCache = false }, TestHost.Engine)
            .RunAsync(relative == null ? Root : Path(relative)).GetAwaiter().GetResult();

    public void Dispose()
    {
        try
        {
            Directory.Delete(Root, recursive: true);
        }
        catch (IOException)
        {
            // A file still open by the OS indexer or antivirus; the temp folder is cleaned up eventually.
        }
    }
}
