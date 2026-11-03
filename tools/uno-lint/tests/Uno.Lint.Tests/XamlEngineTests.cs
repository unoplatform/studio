using System.Xml;
using System.Xml.Linq;
using Microsoft.Extensions.DependencyInjection;
using Uno.Lint.Xaml;
using Uno.XamlParser;
using Uno.XamlParser.Configuration;
using Uno.XamlParser.Stage0;
using Xunit;
using static Uno.Lint.Tests.TestHost;

namespace Uno.Lint.Tests;

/// <summary>
/// The XAML rules run inside Uno.XamlParser. These tests cover what the host adds: linting the malformed markup agents
/// write, the steps another XamlParser host calls, and guards against parser repairs that would corrupt valid files.
/// </summary>
public class XamlEngineTests
{
    private const string Card = "    <Border CornerRadius=\"12\" Background=\"#FF0000\" Padding=\"16\">\n      <StackPanel><TextBlock Text=\"a\"/><TextBlock Text=\"b\"/></StackPanel>\n";

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

    public static TheoryData<string, string, int> BrokenCases() => new()
    {
        { "unclosed element", $"<Page {Ns}>\n  <StackPanel>\n{Card}    </Border>\n    <TextBlock Text=\"hi\">\n  </StackPanel>\n</Page>", 3 },
        { "close tag with wrong casing", $"<Page {Ns}>\n  <StackPanel>\n{Card}    </border>\n  </StackPanel>\n</Page>", 3 },
        { "unquoted attribute", $"<Page {Ns}>\n  <StackPanel>\n    <Border CornerRadius=12 Background=\"#FF0000\" Padding=\"16\">\n      <StackPanel><TextBlock Text=\"a\"/><TextBlock Text=\"b\"/></StackPanel>\n    </Border>\n  </StackPanel>\n</Page>", 3 },
        { "stray ampersand", $"<Page {Ns}>\n  <StackPanel>\n{Card.Replace("Text=\"a\"", "Text=\"Salt & Pepper\"")}    </Border>\n  </StackPanel>\n</Page>", 3 },
        { "truncated output", $"<Page {Ns}>\n  <StackPanel>\n{Card}    </Border>\n", 3 },
        { "double hyphen in a comment", $"<Page {Ns}>\n  <StackPanel>\n{Card}      <!-- header -- footer -->\n    </Border>\n  </StackPanel>\n</Page>", 3 },
    };

    [Theory]
    [MemberData(nameof(BrokenCases))]
    public void Lints_malformed_markup_after_repair(string name, string xaml, int line)
    {
        Assert.False(IsWellFormed(xaml), $"{name}: fixture must be malformed XML");

        var result = AnalyzeXaml(xaml, "Broken.xaml");
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
        var xaml = $$"""
            <Page {{Ns}}>
              <Page.Resources>
                <SolidColorBrush x:Key="AccentBrush" Color="{StaticResource Brand}"/>
              </Page.Resources>
              <StackPanel>
                <Border CornerRadius="8" Background="#FF0000"><Grid/></Border>
                <PathIcon Data="M0,0 L1,1"/>
                <Button uen:Navigation.Request="-"/>
                <TextBox Text="{Binding Name.Value, Mode=TwoWay}"/>
                <Viewbox><Ellipse/></Viewbox>
                <TextBlock Text="unclosed">
              </StackPanel>
            </Page>
            """;
        Assert.False(IsWellFormed(xaml));

        var result = AnalyzeXaml(xaml);
        Assert.True(result.Repaired);
        Assert.Equal(["TOKENTHEME L3", "HEX L6", "CARD L6", "ICON L7", "BACKBAR L8", "BINDVALUE L9", "ICON L10"], result.Findings.Select(f => $"{f.Rule.Name} L{f.Line}"));
    }

    [Fact]
    public void Lints_the_prefix_when_repair_cannot_finish()
    {
        // The sanitizer closes StackPanel before the early </Grid>, which orphans the real </StackPanel>: the reader
        // stops at line 9. The Border on line 6 was read before that and is still linted.
        var xaml = $"<Page {Ns}>\n  <Grid>\n    <StackPanel>\n      <TextBlock Text=\"title\"/>\n    </Grid>\n{Card}    </Border>\n  </StackPanel>\n</Page>";
        var result = AnalyzeXaml(xaml, "Broken.xaml");
        Assert.True(result.Partial);
        Assert.Contains("XML0001", result.RepairCodes);
        Assert.Single(result.Findings, f => f.Rule == Rules.Hex && f.Line == 6);
        Assert.Single(result.Findings, f => f.Rule == Rules.Card && f.Line == 6);
    }

    [Fact]
    public void Malformed_xaml_is_repaired_not_thrown()
    {
        var result = AnalyzeXaml("<Page><Border></Page>", "Bad.xaml");
        Assert.True(result.Parsed);
        Assert.True(result.Repaired);
    }

    // ------------------------------------------------------------ line shifts

    [Fact]
    public void The_repaired_text_is_still_reachable()
    {
        // Line-shift detection reads the repaired text through a non-public property of the parser's string source.
        // A parser update that renames it would silently turn detection off; this pins it.
        Assert.NotNull(XamlLintEngine.RepairedTextProperty);
        var source = XamlStreamSources.FromString("<Page/>", "x.xaml");
        Assert.Equal("<Page/>", XamlLintEngine.RepairedTextProperty!.GetValue(source));
    }

    [Fact]
    public void A_repair_that_removes_a_line_is_reported_as_shifted()
    {
        // The quote repair drops stray text after a '/' up to the next '>', newline included.
        var xaml = $"<Page {Ns}>\n  <Border Background=\"#FF0000\" /\n     junk>\n  <Border Background=\"#00FF00\"/>\n</Page>";
        Assert.False(IsWellFormed(xaml));

        var result = AnalyzeXaml(xaml);
        Assert.True(result.Repaired);
        Assert.True(result.LinesShifted);
    }

    // ------------------------------------------------------------ guards

    [Theory]
    [InlineData("ms-appx:///Assets/BackButton.png")]
    [InlineData("see http://example.com/help")]
    public void Valid_xaml_with_urls_in_text_is_read_to_the_end(string text)
    {
        // Before Uno.XamlParser 0.10.0-dev.80 a Stage-0 sanitizer rewrote valid markup when element text contained ":/"
        // and the reader stopped there. Well-formed files still go through a pipeline without sanitizers.
        var xaml = $"<ResourceDictionary {Ns}>\n  <x:String x:Key=\"Image\">{text}</x:String>\n  <SolidColorBrush x:Key=\"InkBrush\" Color=\"#FF0000\"/>\n</ResourceDictionary>";
        Assert.True(IsWellFormed(xaml));

        var result = AnalyzeXaml(xaml, "App.xaml");
        Assert.False(result.Repaired);
        Assert.False(result.Partial);
        var hit = Assert.Single(result.Findings);
        Assert.Equal(Rules.Hex, hit.Rule);
        Assert.Equal(3, hit.Line);
    }

    [Fact]
    public void A_document_type_declaration_is_never_expanded()
    {
        var secret = Path.Combine(Path.GetTempPath(), $"uno-lint-secret-{Guid.NewGuid():N}.txt");
        File.WriteAllText(secret, "SECRET-FILE-CONTENT");
        try
        {
            var xaml = $$"""
                <!DOCTYPE Page [
                  <!ENTITY internal "INTERNAL-EXPANDED">
                  <!ENTITY ext SYSTEM "{{new Uri(secret)}}">
                ]>
                <Page {{Ns}}>
                  <Border x:Name="&internal;" CornerRadius="8" Background="#FF0000"><Grid/></Border>
                  <Border x:Name="&ext;" CornerRadius="8" Background="#00FF00"><Grid/></Border>
                </Page>
                """;
            Assert.False(UnoLintXaml.IsWellFormed(xaml), "XAML has no DTD; one makes the file malformed");

            var result = AnalyzeXaml(xaml);
            Assert.All(result.Findings, f => Assert.DoesNotContain("EXPANDED", f.Message));
            Assert.All(result.Findings, f => Assert.DoesNotContain("SECRET", f.Message));
        }
        finally
        {
            File.Delete(secret);
        }
    }

    [Fact]
    public async Task Concurrent_calls_are_serialized_and_give_the_same_findings()
    {
        var xaml = $"<Page {Ns}>\n<Border CornerRadius=\"8\" Background=\"#FF0000\"><Grid/></Border>\n<TextBox Text=\"{{Binding Name.Value}}\"/>\n</Page>";
        var calls = Enumerable.Range(0, 16).Select(i => Engine.AnalyzeAsync($"Page{i}.xaml", xaml, _ => true).AsTask());
        var results = await Task.WhenAll(calls);
        Assert.All(results, r => Assert.Equal(["HEX L2", "CARD L2", "BINDVALUE L3"], r.Findings.Select(f => $"{f.Rule.Name} L{f.Line}")));
    }

    [Fact]
    public async Task A_call_waiting_for_the_engine_can_be_cancelled()
    {
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Engine.AnalyzeAsync("x.xaml", "<Page/>", _ => true, cancelled.Token).AsTask());
    }

    // ------------------------------------------------------------ another XamlParser host

    /// <summary>What Studio Live or Uno.AI would do: their own pipeline with default fixers, plus the three steps.</summary>
    private static async Task<XamlFileFindings> LintInStockPipeline(string xaml, string fileName)
    {
        var services = new ServiceCollection();
        var sanitize = !UnoLintXaml.IsWellFormed(xaml);
        services.AddXamlStreamPipeline(options =>
        {
            options.UseDefaultSanitizers = sanitize;
            UnoLintXaml.AddTo(options);
        }).AddXamlProcessingPipeline();
        var pipeline = services.BuildServiceProvider().GetRequiredService<XamlProcessingPipeline>();
        var result = await pipeline.ProcessAsync(XamlStreamSources.FromString(xaml, fileName));
        return UnoLintXaml.Collect(result, fileName, xaml, _ => true);
    }

    [Fact]
    public async Task Rules_plug_into_a_stock_xamlparser_pipeline()
    {
        var xaml = $"<Page {Ns}>\n  <Border CornerRadius=\"8\" Background=\"#FF0000\"><Grid/></Border>\n</Page>";
        var result = await LintInStockPipeline(xaml, "MainPage.xaml");
        Assert.Equal(["HEX L2", "CARD L2"], result.Findings.Select(f => $"{f.Rule.Name} L{f.Line}"));
    }

    [Fact]
    public async Task Collect_reports_a_literal_single_themed_brush_once_in_any_host()
    {
        var xaml = $"<ResourceDictionary {Ns}>\n  <SolidColorBrush x:Key=\"StatusBrush\"\n      Color=\"#18D96A\"/>\n</ResourceDictionary>";
        var hit = Assert.Single((await LintInStockPipeline(xaml, "App.xaml")).Findings);
        Assert.Equal(Rules.Hex, hit.Rule);
        Assert.Contains("StatusBrush is also defined once for both themes", hit.Message);
    }

    [Fact]
    public async Task Collect_applies_suppression_comments_in_any_host()
    {
        var xaml = $"<Page {Ns}>\n  <!-- uno-lint: allow hex - chart series -->\n  <Border Background=\"#FF0000\"/>\n</Page>";
        Assert.True(Assert.Single((await LintInStockPipeline(xaml, "MainPage.xaml")).Findings).Suppressed);
    }

    [Fact]
    public async Task The_well_formedness_gate_keeps_urls_in_text_from_cutting_the_file_short()
    {
        var xaml = $"<ResourceDictionary {Ns}>\n  <x:String x:Key=\"Logo\">ms-appx:///Assets/logo.png</x:String>\n  <SolidColorBrush x:Key=\"InkInvariantBrush\" Color=\"#FF0000\"/>\n</ResourceDictionary>";
        var hit = Assert.Single((await LintInStockPipeline(xaml, "App.xaml")).Findings);
        Assert.Equal(3, hit.Line);
    }
}
