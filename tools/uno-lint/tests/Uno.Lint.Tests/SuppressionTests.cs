using Xunit;
using static Uno.Lint.Tests.TestHost;

namespace Uno.Lint.Tests;

/// <summary>Allow comments: where they reach, what they need, how they are counted, and line endings.</summary>
public class SuppressionTests
{
    [Fact]
    public void An_allow_comment_with_a_reason_suppresses_the_hit_below_it()
    {
        Assert.DoesNotContain(LintXaml($"<Page {Ns}>\n<!-- uno-lint: allow hex - chart series colour -->\n<Border Background=\"#FF0000\"/>\n</Page>"), x => x.Rule == Rules.Hex);
        Assert.DoesNotContain(LintXaml($"<Page {Ns}>\n<!-- xaml-lint: allow hex - chart series colour -->\n<Border Background=\"#FF0000\"/>\n</Page>"), x => x.Rule == Rules.Hex);
    }

    [Theory]
    [InlineData("<!-- uno-lint: allow hex -->")]
    [InlineData("<!-- uno-lint: allow hex - -->")]
    [InlineData("<!-- uno-lint: allow card - wrong rule -->")]
    public void An_allow_comment_without_a_reason_or_for_another_rule_does_not(string comment)
    {
        Assert.Single(LintXaml($"<Page {Ns}>\n{comment}\n<Border Background=\"#FF0000\"/>\n</Page>"), x => x.Rule == Rules.Hex);
    }

    [Fact]
    public void Allow_all_covers_every_rule()
    {
        Assert.Empty(LintXaml($"<Page {Ns}>\n<!-- uno-lint: allow all - legacy hero, tracked in the backlog -->\n<Border CornerRadius=\"8\" Background=\"#FF0000\"/>\n</Page>"));
    }

    [Fact]
    public void An_allow_comment_above_a_multiline_element_covers_its_attributes()
    {
        var xaml = $$"""
            <Page {{Ns}}>
              <!-- uno-lint: allow hex - brand chart color -->
              <Border
                  Width="100"
                  Height="100"
                  Margin="4"
                  Background="#FF7A67F8" />
              <Border Background="#FF000000" />
            </Page>
            """;
        var all = LintXaml(xaml, includeSuppressed: true).Where(f => f.Rule == Rules.Hex).ToList();
        Assert.Equal(2, all.Count);
        Assert.True(all.Single(f => f.Line == 7).Suppressed);
        Assert.False(all.Single(f => f.Line == 8).Suppressed);
    }

    [Fact]
    public void An_allow_comment_stops_at_the_previous_element()
    {
        var xaml = $$"""
            <Page {{Ns}}>
              <Page.Resources>
                <!-- uno-lint: allow hex - chart series -->
                <SolidColorBrush x:Key="SeriesInvariantBrush" Color="#FF0000"/>
              </Page.Resources>
              <Border Background="#00FF00"/>
            </Page>
            """;
        var all = LintXaml(xaml, includeSuppressed: true).Where(f => f.Rule == Rules.Hex).ToList();
        Assert.Equal(2, all.Count);
        Assert.True(all.Single(f => f.Line == 4).Suppressed);
        Assert.False(all.Single(f => f.Line == 6).Suppressed);
    }

    [Fact]
    public void An_allow_comment_reaches_through_consecutive_comment_lines()
    {
        var code = """
            partial class P : Page {
              // The transport bar binds to a MediaPlayerElement that has no command surface.
              // uno-lint: allow codebehind - media transport glue
              // See issue #123 for the upstream request.
              void OnPositionChanged(object sender, RoutedEventArgs e) { }
              void OnOther(object sender, RoutedEventArgs e) { }
            }
            """;
        Assert.Equal(["handler OnOther"], LintCs(code).Where(f => f.Rule == Rules.CodeBehind).Select(f => f.Message));
    }

    [Theory]
    [InlineData("\r")]
    [InlineData("\n")]
    [InlineData("\r\n")]
    public void Every_line_ending_puts_findings_and_allow_comments_on_the_same_lines(string newline)
    {
        // A lone CR ends a line for XmlReader and Roslyn; it must for the suppression look-behind too.
        var xaml = string.Join(newline, $"<Page {Ns}>", "<!-- uno-lint: allow hex - chart series -->", "<Border Background=\"#FF0000\"/>", "<Border Background=\"#00FF00\"/>", "</Page>");
        var all = LintXaml(xaml, includeSuppressed: true).Where(f => f.Rule == Rules.Hex).ToList();
        Assert.True(all.Single(f => f.Line == 3).Suppressed);
        Assert.False(all.Single(f => f.Line == 4).Suppressed);

        var code = string.Join(newline, "partial class P : Page {", "// uno-lint: allow codebehind - glue", "void A(object sender, EventArgs e) { }", "void B(object sender, EventArgs e) { }", "}");
        Assert.Equal(["handler B"], LintCs(code).Select(f => f.Message));
    }

    [Fact]
    public void Mixed_line_endings_keep_an_allow_comment_with_its_line()
    {
        var code = "partial class P : Page {\r\n// uno-lint: allow codebehind - glue\rvoid A(object sender, EventArgs e) { }\nvoid B(object sender, EventArgs e) { }\r\n}";
        Assert.Equal(["handler B"], LintCs(code).Select(f => f.Message));
    }

    [Fact]
    public void Anchors_outside_the_file_are_ignored()
    {
        Assert.False(Suppression.IsSuppressed(["<!-- uno-lint: allow hex - x -->"], Rules.Hex, 0, -3, 9));
        Assert.True(Suppression.IsSuppressed(["<!-- uno-lint: allow hex - x -->", "<Border/>"], Rules.Hex, 2));
    }

    [Fact]
    public void Suppressed_hits_are_counted_not_reported()
    {
        using var project = new TempProject();
        project.Write("App.csproj", "<Project Sdk=\"Uno.Sdk\"><PropertyGroup><UnoFeatures>Toolkit</UnoFeatures></PropertyGroup></Project>");
        // The allow comment reaches three non-blank comment lines, so the second Border sits further down.
        project.Write("MainPage.xaml", $"{Page}\n<!-- uno-lint: allow hex - chart series -->\n<Border Background=\"#FF0000\"/>\n<TextBlock/>\n<TextBlock/>\n<TextBlock/>\n<Border Background=\"#00FF00\"/>\n</Page>");

        var result = project.Lint();
        Assert.Single(result.Findings);
        var suppressed = Assert.Single(result.Suppressed);
        Assert.Equal(Rules.Hex, suppressed.Rule);
    }

    [Fact]
    public void Suppressed_hits_of_disabled_rules_are_not_counted()
    {
        using var project = new TempProject();
        project.Write("App.csproj", "<Project/>");
        project.Write("MainPage.xaml.cs", "partial class P : Page {\n// uno-lint: allow codebehind - glue\nvoid H(object sender, EventArgs e) { } }");

        var result = project.Lint();
        Assert.Empty(result.Findings);
        Assert.Empty(result.Suppressed);
    }

    [Fact]
    public void A_trailing_allow_comment_covers_its_own_line_only()
    {
        var xaml = $"<Page {Ns}>\n<Border Background=\"Red\"/> <!-- uno-lint: allow hex - this line only -->\n<Border Background=\"Blue\"/>\n</Page>";
        var all = LintXaml(xaml, includeSuppressed: true).Where(f => f.Rule == Rules.Hex).ToList();
        Assert.True(all.Single(f => f.Line == 2).Suppressed);
        Assert.False(all.Single(f => f.Line == 3).Suppressed);
    }

    [Fact]
    public void An_allow_comment_does_not_reach_past_an_element_with_a_trailing_comment()
    {
        var xaml = $"<Page {Ns}>\n<!-- uno-lint: allow hex - grid only -->\n<Grid/> <!-- note -->\n<Border Background=\"Blue\"/>\n</Page>";
        Assert.False(Assert.Single(LintXaml(xaml, includeSuppressed: true), f => f.Rule == Rules.Hex).Suppressed);
    }

    [Fact]
    public void An_allow_line_inside_a_multiline_comment_counts()
    {
        var xaml = $"<Page {Ns}>\n<!--\n  uno-lint: allow hex - brand green\n-->\n<Border Background=\"Green\"/>\n</Page>";
        Assert.True(Assert.Single(LintXaml(xaml, includeSuppressed: true), f => f.Rule == Rules.Hex).Suppressed);

        var code = "partial class P : Page {\n/*\n uno-lint: allow codebehind - glue\n*/\nvoid A(object sender, EventArgs e) { }\n}";
        Assert.Empty(LintCs(code));
    }

    [Theory]
    [InlineData("\u0085")]
    [InlineData("\u2028")]
    [InlineData("\u2029")]
    public void Csharp_lines_end_where_roslyn_ends_them(string separator)
    {
        var code = $"class A {{\n // s{separator}x\n void F(){{ var c = Colors.Red; }} }}";
        Assert.Equal(4, Assert.Single(LintCs(code, "A.cs"), f => f.Rule == Rules.Hex).Line);

        var allowed = $"class A {{\n /* {separator} */\n // uno-lint: allow hex - brand\n void F(){{ var c = Colors.Red; }} }}";
        Assert.True(Assert.Single(LintCs(allowed, "A.cs", includeSuppressed: true), f => f.Rule == Rules.Hex).Suppressed);
    }
}
