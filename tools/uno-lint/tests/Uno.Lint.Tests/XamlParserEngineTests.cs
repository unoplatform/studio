using System.Xml;
using System.Xml.Linq;
using Microsoft.Extensions.DependencyInjection;
using Uno.Lint;
using Uno.Lint.Xaml;
using Uno.XamlParser;
using Uno.XamlParser.Configuration;
using Uno.XamlParser.Stage0;
using Xunit;

namespace Uno.Lint.Tests;

/// <summary>
/// The XAML rules run inside Uno.XamlParser as Stage-1 fixers. These tests cover what that host adds: linting the
/// malformed markup agents write, plugging the rules into any XamlParser pipeline, and guarding against parser repairs
/// that would corrupt valid files.
/// </summary>
public class XamlParserEngineTests : IDisposable
{
    private const string Ns = "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" xmlns:utu=\"using:Uno.Toolkit.UI\" xmlns:uen=\"using:Uno.Extensions.Navigation.UI\"";

    private static readonly XamlParserEngine Engine = XamlTestHost.Engine;
    private static readonly ProjectContext Everything = new() { HasToolkit = true, HasUnoThemes = true, HasMvux = true };

    private readonly string _root = Path.Combine(Path.GetTempPath(), "uno-lint-tests", Guid.NewGuid().ToString("N"));

    public XamlParserEngineTests()
    {
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch { }
    }

    private static bool Strict(RuleDescriptor rule) => Rules.Resolve(rule, new LintOptions { Profile = Profile.Strict }, Everything) != Severity.None;

    private static bool IsWellFormed(string xaml)
    {
        try
        {
            XDocument.Parse(xaml);
            return true;
        }
        catch (XmlException)
        {
            return false;
        }
    }

    // ------------------------------------------------------------ broken markup

    private const string Card = "    <Border CornerRadius=\"12\" Background=\"#FF0000\" Padding=\"16\">\n      <StackPanel><TextBlock Text=\"a\"/><TextBlock Text=\"b\"/></StackPanel>\n";

    public static IEnumerable<object[]> BrokenCases()
    {
        yield return new object[] { "unclosed element", $"<Page {Ns}>\n  <StackPanel>\n{Card}    </Border>\n    <TextBlock Text=\"hi\">\n  </StackPanel>\n</Page>", 3 };
        yield return new object[] { "close tag with wrong casing", $"<Page {Ns}>\n  <StackPanel>\n{Card}    </border>\n  </StackPanel>\n</Page>", 3 };
        yield return new object[] { "unquoted attribute", $"<Page {Ns}>\n  <StackPanel>\n    <Border CornerRadius=12 Background=\"#FF0000\" Padding=\"16\">\n      <StackPanel><TextBlock Text=\"a\"/><TextBlock Text=\"b\"/></StackPanel>\n    </Border>\n  </StackPanel>\n</Page>", 3 };
        yield return new object[] { "stray ampersand", $"<Page {Ns}>\n  <StackPanel>\n{Card.Replace("Text=\"a\"", "Text=\"Salt & Pepper\"")}    </Border>\n  </StackPanel>\n</Page>", 3 };
        yield return new object[] { "truncated output", $"<Page {Ns}>\n  <StackPanel>\n{Card}    </Border>\n", 3 };
        yield return new object[] { "double hyphen in a comment", $"<Page {Ns}>\n  <StackPanel>\n{Card}      <!-- header -- footer -->\n    </Border>\n  </StackPanel>\n</Page>", 3 };
    }

    [Theory]
    [MemberData(nameof(BrokenCases))]
    public void Lints_malformed_markup_after_repair(string name, string xaml, int line)
    {
        Assert.False(IsWellFormed(xaml), $"{name}: fixture must be malformed XML");

        var result = Engine.Analyze("Broken.xaml", xaml, Everything, Strict);
        Assert.True(result.Parsed, name);
        Assert.True(result.Repaired, name);
        Assert.False(result.LinesShifted, name);
        Assert.Single(result.Findings, f => f.Rule == Rules.Hex && f.Line == line);
        Assert.Single(result.Findings, f => f.Rule == Rules.Card && f.Line == line);
    }

    [Fact]
    public void Every_xaml_rule_runs_on_repaired_markup()
    {
        // One unclosed TextBlock (line 11) makes the file malformed; every rule must still fire on its own line.
        var xaml = $@"<Page {Ns}>
  <Page.Resources>
    <SolidColorBrush x:Key=""AccentBrush"" Color=""{{StaticResource Brand}}""/>
  </Page.Resources>
  <StackPanel>
    <Border CornerRadius=""8"" Background=""#FF0000""><Grid/></Border>
    <PathIcon Data=""M0,0 L1,1""/>
    <Button uen:Navigation.Request=""-""/>
    <TextBox Text=""{{Binding Name.Value, Mode=TwoWay}}""/>
    <Viewbox><Ellipse/></Viewbox>
    <TextBlock Text=""unclosed"">
  </StackPanel>
</Page>";
        Assert.False(IsWellFormed(xaml));

        var result = Engine.Analyze("MainPage.xaml", xaml, Everything, Strict);
        Assert.True(result.Repaired);
        var found = result.Findings.Select(f => $"{f.Rule.Name} L{f.Line}").OrderBy(s => s, StringComparer.Ordinal);
        Assert.Equal(new[] { "BACKBAR L8", "BINDVALUE L9", "CARD L6", "HEX L6", "ICON L10", "ICON L7", "TOKENTHEME L3" }, found);
    }

    [Fact]
    public void Lints_the_prefix_when_repair_cannot_finish()
    {
        // The sanitizer closes StackPanel before the early </Grid>, which orphans the real </StackPanel>: the reader
        // stops at line 9. The Border on line 6 was read before that and is still linted.
        var xaml = $"<Page {Ns}>\n  <Grid>\n    <StackPanel>\n      <TextBlock Text=\"title\"/>\n    </Grid>\n{Card}    </Border>\n  </StackPanel>\n</Page>";
        var result = Engine.Analyze("Broken.xaml", xaml, Everything, Strict);
        Assert.True(result.Partial);
        Assert.Contains("XML0001", result.RepairCodes);
        Assert.Single(result.Findings, f => f.Rule == Rules.Hex && f.Line == 6);
        Assert.Single(result.Findings, f => f.Rule == Rules.Card && f.Line == 6);
    }

    // ------------------------------------------------------------ guards

    [Theory]
    [InlineData("ms-appx:///Assets/BackButton.png")]
    [InlineData("see http://example.com/help")]
    public void Valid_xaml_with_urls_in_text_is_read_to_the_end(string text)
    {
        // In Uno.XamlParser 0.10.0-dev.74 the quote-repair sanitizer rewrites valid markup when element text contains
        // ":/" and the reader stops there. Well-formed files go through a pipeline without sanitizers.
        var xaml = $"<ResourceDictionary {Ns}>\n  <x:String x:Key=\"Image\">{text}</x:String>\n  <SolidColorBrush x:Key=\"InkBrush\" Color=\"#FF0000\"/>\n</ResourceDictionary>";
        Assert.True(IsWellFormed(xaml));

        var result = Engine.Analyze("App.xaml", xaml, Everything, Strict);
        Assert.False(result.Repaired);
        Assert.False(result.Partial);
        var hit = Assert.Single(result.Findings);
        Assert.Equal(Rules.Hex, hit.Rule);
        Assert.Equal(3, hit.Line);
    }

    [Fact]
    public void A_resource_key_named_like_a_color_is_not_a_color()
    {
        // <SolidColorBrush x:Key="Red">Red</SolidColorBrush>: the text is the color; the key only names the resource.
        var xaml = $"<ResourceDictionary {Ns}>\n  <SolidColorBrush x:Key=\"Red\">Red</SolidColorBrush>\n</ResourceDictionary>";
        var hit = Assert.Single(Engine.Analyze("App.xaml", xaml, Everything, Strict).Findings);
        Assert.Equal("literal Red in <SolidColorBrush>", hit.Message);
    }

    // ------------------------------------------------------------ reuse in another XamlParser host

    [Fact]
    public async Task Rules_plug_into_a_stock_xamlparser_pipeline()
    {
        // What Studio Live or Uno.AI would do: their own pipeline, default fixers and sanitizers on, plus one call.
        var services = new ServiceCollection();
        services.AddXamlStreamPipeline(UnoLintXamlRules.AddTo).AddXamlProcessingPipeline();
        var pipeline = services.BuildServiceProvider().GetRequiredService<XamlProcessingPipeline>();

        var xaml = $"<Page {Ns}>\n  <Border CornerRadius=\"8\" Background=\"#FF0000\"><Grid/></Border>\n</Page>";
        var result = await pipeline.ProcessAsync(XamlStreamSources.FromString(xaml, "MainPage.xaml"));

        var codes = result.SemanticResult!.Diagnostics.Where(d => d.Code.StartsWith("UNOL", StringComparison.Ordinal)).Select(d => $"{d.Code} L{d.LineInfo.LineNumber}");
        Assert.Equal(new[] { "UNOL001 L2", "UNOL101 L2" }, codes.OrderBy(s => s, StringComparer.Ordinal));
    }

    // ------------------------------------------------------------ host integration

    private void Write(string relative, string content)
    {
        var path = Path.Combine(_root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }

    [Fact]
    public void Linter_counts_repaired_files_and_reports_each_hit_once()
    {
        Write("App.csproj", "<Project Sdk=\"Uno.Sdk\"><PropertyGroup><UnoFeatures>Material;Toolkit</UnoFeatures></PropertyGroup></Project>");
        Write("Valid.xaml", $"<Page {Ns}>\n<Page.Resources><SolidColorBrush x:Key=\"StatusBrush\" Color=\"#18D96A\"/></Page.Resources>\n<Border CornerRadius=\"8\" Background=\"White\"><Grid/></Border>\n<Path Data=\"M0,0 L1,1\"/>\n</Page>");
        Write("Broken.xaml", $"<Page {Ns}>\n  <StackPanel>\n{Card}    </border>\n  </StackPanel>\n</Page>");

        var result = new Linter(new LintOptions { Profile = Profile.Strict }).Run(_root);

        Assert.Equal(0, result.UnparsedXamlFiles);
        Assert.Equal(1, result.RepairedXamlFiles);

        // The literal brush is one HEX finding with a single-theme note, not also a TOKENTHEME one.
        Assert.DoesNotContain(result.Findings, f => f.Rule == Rules.TokenTheme);
        Assert.Equal(
            new[] { "CARD L3", "HEX L2", "HEX L3", "ICON L4" },
            result.Findings.Where(f => f.File == "Valid.xaml").Select(f => $"{f.Rule.Name} L{f.Line}").OrderBy(s => s, StringComparer.Ordinal));
        Assert.Equal(
            new[] { "CARD L3", "HEX L3" },
            result.Findings.Where(f => f.File == "Broken.xaml").Select(f => $"{f.Rule.Name} L{f.Line}").OrderBy(s => s, StringComparer.Ordinal));
    }
}
