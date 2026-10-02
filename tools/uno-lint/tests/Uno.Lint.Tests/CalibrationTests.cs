using Uno.Lint;
using Xunit;

namespace Uno.Lint.Tests;

/// <summary>Regression tests for false positives found while running the tool on real apps.</summary>
public class CalibrationTests
{
    private const string Ns = "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" xmlns:utu=\"using:Uno.Toolkit.UI\"";

    private static List<Finding> LintXaml(string xaml, string fileName = "MainPage.xaml")
    {
        Assert.True(XamlFile.TryParse(fileName, xaml, out var file));
        var ctx = new ProjectContext { HasToolkit = true };
        var options = new LintOptions { Profile = Profile.Strict };
        return XamlAnalyzer.Analyze(file!, ctx, r => Rules.Resolve(r, options, ctx) != Severity.None).ToList();
    }

    private static List<Finding> LintCs(string code, string fileName = "MainPage.xaml.cs")
    {
        var ctx = new ProjectContext { HasToolkit = true };
        var options = new LintOptions { Profile = Profile.Strict };
        return CSharpAnalyzer.Analyze(new CSharpFile(fileName, code, null), ctx, r => Rules.Resolve(r, options, ctx) != Severity.None).ToList();
    }

    [Fact]
    public void Card_skips_partial_corner_radius_swatches()
    {
        // Toolkit SeedColorSamplePage: a grid of colour swatches with rounded outer corners only.
        var xaml = $@"<Page {Ns}>
  <Border Background=""{{ThemeResource PrimaryBrush}}"" CornerRadius=""8,0,0,0"" Padding=""8""/>
  <Border Background=""{{ThemeResource PrimaryBrush}}"" CornerRadius=""0,0,8,0"" Padding=""8""/>
  <Border Background=""{{ThemeResource PrimaryBrush}}"" CornerRadius=""8"" Padding=""8""/>
  <Border Background=""{{ThemeResource PrimaryBrush}}"" CornerRadius=""8,8,8,8"" Padding=""8""/>
</Page>";
        Assert.Equal(2, LintXaml(xaml).Count(f => f.Rule == Rules.Card));
    }

    [Fact]
    public void Responsive_ignores_has_layout_run_checks_against_zero()
    {
        // Toolkit SafeArea_SoftInput_Scroll: "if (Spacer.ActualHeight > 0)" is not a breakpoint.
        var code = @"partial class P : Page {
  void OnSizeChanged(object sender, SizeChangedEventArgs e) {
    if (Spacer.ActualHeight > 0) { }
    if (0 < Spacer.ActualHeight) { }
    if (e.NewSize.Width >= 720) { }
  } }";
        Assert.Single(LintCs(code).Where(f => f.Rule == Rules.Responsive));
    }

    [Fact]
    public void Hex_literal_on_a_single_valued_brush_is_reported_once_as_TokenTheme()
    {
        // GridWatch App.xaml: 18 brushes each produced a HEX and a TOKENTHEME hit on the same line.
        var xaml = $@"<ResourceDictionary {Ns}>
  <SolidColorBrush x:Key=""StatusOnlineBrush"" Color=""#18D96A""/>
  <SolidColorBrush x:Key=""StatusWarningBrush"" Color=""#F59E0B""/>
</ResourceDictionary>";
        var findings = LintXaml(xaml, "App.xaml");
        Assert.Equal(2, findings.Count);
        Assert.All(findings, f => Assert.Equal(Rules.TokenTheme, f.Rule));
        Assert.Contains(findings, f => f.Message.Contains("StatusOnlineBrush") && f.Message.Contains("#18D96A"));
    }

    [Fact]
    public void EditRange_fallback_locates_added_text_across_line_ending_and_indent_differences()
    {
        var full = "line1\r\n    <Border\r\n        CornerRadius=\"8\"/>\r\nline4\r\n";

        Assert.True(EditRange.TryLocate(full, "", "    <Border\n        CornerRadius=\"8\"/>", false, out var lines));
        Assert.Equal(new[] { 2, 3 }, lines.OrderBy(l => l));

        Assert.True(EditRange.TryLocate(full, "", "<Border CornerRadius=\"8\"/>", false, out lines));
        Assert.Equal(new[] { 2 }, lines.OrderBy(l => l));

        Assert.False(EditRange.TryLocate(full, "", "<Grid/>", false, out _));
    }
}
