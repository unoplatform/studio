using Uno.Lint;
using Uno.Lint.Xaml;
using Xunit;

namespace Uno.Lint.Tests;

/// <summary>
/// The Uno.XamlParser host must agree with the built-in host on valid XAML and keep linting where the built-in host
/// gives up: the malformed markup agents write.
/// </summary>
public class XamlParserEngineTests : IDisposable
{
    private const string Ns = "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" xmlns:utu=\"using:Uno.Toolkit.UI\"";

    private static readonly XamlParserEngine Engine = new XamlParserEngine();

    private readonly string _root = Path.Combine(Path.GetTempPath(), "uno-lint-tests", Guid.NewGuid().ToString("N"));

    public XamlParserEngineTests()
    {
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch { }
    }

    private static readonly ProjectContext Toolkit = new ProjectContext { HasToolkit = true, HasUnoThemes = true };

    private static bool Strict(RuleDescriptor rule) => Rules.Resolve(rule, new LintOptions { Profile = Profile.Strict }, Toolkit) != Severity.None;

    private static string Key(Finding f) => $"{f.Rule.Name} L{f.Line} {(f.Suppressed ? "SUPP " : "")}{f.Message}";

    private static List<string> BuiltIn(string xaml, string file)
    {
        Assert.True(XamlFile.TryParse(file, xaml, out var parsed), "fixture must be valid XML for the built-in host");
        return XamlAnalyzer.Analyze(parsed!, Toolkit, Strict)
            .Where(f => Engine.OwnedRules.Contains(f.Rule))
            .Select(Key).OrderBy(k => k, StringComparer.Ordinal).ToList();
    }

    private static List<string> Parser(string xaml, string file) =>
        Engine.Analyze(file, xaml, Toolkit, Strict).Findings.Select(Key).OrderBy(k => k, StringComparer.Ordinal).ToList();

    // ------------------------------------------------------------ parity on valid XAML

    public static IEnumerable<object[]> ParityCases()
    {
        yield return new object[] { "attributes", $@"<Page {Ns}>
  <Border Background=""#FF0000"" BorderBrush=""White"" Foreground=""Transparent""/>
  <TextBlock Text=""#404"" Tag=""#ABC123"" Foreground=""Red""/>
</Page>", "MainPage.xaml" };

        yield return new object[] { "setters, animations, gradients, visual states", $@"<Page {Ns}>
  <Style TargetType=""Border""><Setter Property=""Background"" Value=""#111111""/></Style>
  <Style TargetType=""Border""><Setter Property=""BorderBrush""><Setter.Value>#222222</Setter.Value></Setter></Style>
  <ObjectAnimationUsingKeyFrames Storyboard.TargetProperty=""(Border.Background)""><DiscreteObjectKeyFrame Value=""#333333""/></ObjectAnimationUsingKeyFrames>
  <ColorAnimation To=""#444444""/>
  <LinearGradientBrush><GradientStop Color=""#555555"" Offset=""0""/></LinearGradientBrush>
  <VisualStateManager.VisualStateGroups><VisualStateGroup><VisualState><VisualState.Setters>
    <Setter Target=""Hero.Background"" Value=""#999999""/>
    <Setter Target=""Title.Text"" Value=""#404""/>
  </VisualState.Setters></VisualState></VisualStateGroup></VisualStateManager.VisualStateGroups>
</Page>", "MainPage.xaml" };

        yield return new object[] { "resources and theme dictionaries", $@"<ResourceDictionary {Ns}>
  <ResourceDictionary.ThemeDictionaries>
    <ResourceDictionary x:Key=""Light"">
      <LinearGradientBrush x:Key=""HeroBrush""><GradientStop Color=""#FF0000"" Offset=""0""/></LinearGradientBrush>
      <SolidColorBrush x:Key=""InkBrush""><SolidColorBrush.Color>#222222</SolidColorBrush.Color></SolidColorBrush>
      <SolidColorBrush Color=""#444444""/>
    </ResourceDictionary>
  </ResourceDictionary.ThemeDictionaries>
  <Color x:Key=""Brand"">#7A67F8</Color>
  <SolidColorBrush x:Key=""StatusBrush""
                   Color=""#18D96A""/>
  <SolidColorBrush x:Key=""ScrimInvariantBrush"" Color=""Black""/>
</ResourceDictionary>", "App.xaml" };

        yield return new object[] { "suppressions", $@"<Page {Ns}>
  <Page.Resources>
    <!-- uno-lint: allow hex - chart series -->
    <SolidColorBrush x:Key=""SeriesInvariantBrush"" Color=""#FF0000""/>
  </Page.Resources>
  <!-- uno-lint: allow hex - brand chart color -->
  <Border
      Width=""100""
      Background=""#FF7A67F8"" />
  <Border Background=""#00FF00""/>
</Page>", "MainPage.xaml" };

        yield return new object[] { "cards", $@"<Page {Ns}>
  <Border CornerRadius=""8"" Background=""Red"" x:Name=""Hero""/>
  <Border CornerRadius=""0"" Background=""{{ThemeResource SurfaceBrush}}""/>
  <Border CornerRadius=""8,0,0,0"" Padding=""8"" Background=""White""/>
  <Border Width=""48"" Height=""48"" CornerRadius=""24"" Background=""White""/>
  <Border Height=""32"" CornerRadius=""16"" Padding=""12,0"" Background=""White""><TextBlock Text=""New""/></Border>
  <Border CornerRadius=""20"" Padding=""12,6"" Background=""White""><TextBlock Text=""Oat milk""/></Border>
  <Border CornerRadius=""16"" Padding=""20,16"" Background=""White""><StackPanel><TextBlock/><TextBlock/></StackPanel></Border>
  <Border CornerRadius=""{{StaticResource CardCornerRadius}}"" Background=""White""><Grid/></Border>
  <Style TargetType=""Button""><Setter Property=""Template""><Setter.Value>
    <ControlTemplate TargetType=""Button""><Border CornerRadius=""4"" Background=""Red""/></ControlTemplate>
  </Setter.Value></Setter></Style>
  <utu:ShadowContainer><Border CornerRadius=""8"" Padding=""4""/></utu:ShadowContainer>
</Page>", "MainPage.xaml" };

        yield return new object[] { "palette file", $"<ResourceDictionary {Ns}><Color x:Key=\"Brand\">#7A67F8</Color><SolidColorBrush x:Key=\"BrandBrush\" Color=\"Red\"/></ResourceDictionary>", "AppColors.xaml" };
    }

    [Theory]
    [MemberData(nameof(ParityCases))]
    public void Matches_the_builtin_host_on_valid_xaml(string name, string xaml, string file)
    {
        var builtIn = BuiltIn(xaml, file);
        var parser = Parser(xaml, file);
        Assert.True(builtIn.SequenceEqual(parser), $"{name}\nbuilt-in:\n  {string.Join("\n  ", builtIn)}\nparser:\n  {string.Join("\n  ", parser)}");
        // Every case but the palette file must exercise the rules, or parity would be vacuous.
        Assert.Equal(name == "palette file", builtIn.Count == 0);
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
    public void Lints_markup_the_builtin_host_rejects(string name, string xaml, int line)
    {
        Assert.False(XamlFile.TryParse("Broken.xaml", xaml, out _), $"{name}: fixture must be invalid XML");

        var result = Engine.Analyze("Broken.xaml", xaml, Toolkit, Strict);
        Assert.True(result.Parsed, name);
        Assert.True(result.Repaired, name);
        Assert.False(result.LinesShifted, name);
        Assert.Single(result.Findings, f => f.Rule == Rules.Hex && f.Line == line);
        Assert.Single(result.Findings, f => f.Rule == Rules.Card && f.Line == line);
    }

    [Fact]
    public void Lints_the_prefix_when_repair_cannot_finish()
    {
        // The sanitizer closes StackPanel before the early </Grid>, which orphans the real </StackPanel>: the reader
        // stops at line 9. The Border on line 6 was read before that and is still linted.
        var xaml = $"<Page {Ns}>\n  <Grid>\n    <StackPanel>\n      <TextBlock Text=\"title\"/>\n    </Grid>\n{Card.Replace("    <Border", "    <Border")}    </Border>\n  </StackPanel>\n</Page>";
        var result = Engine.Analyze("Broken.xaml", xaml, Toolkit, Strict);
        Assert.True(result.Partial);
        Assert.Contains("XML0001", result.RepairCodes);
        Assert.Single(result.Findings, f => f.Rule == Rules.Hex && f.Line == 6);
        Assert.Single(result.Findings, f => f.Rule == Rules.Card && f.Line == 6);
    }

    // ------------------------------------------------------------ host integration

    private void Write(string relative, string content)
    {
        var path = Path.Combine(_root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }

    [Fact]
    public void Linter_counts_repaired_files_and_runs_each_rule_once()
    {
        Write("App.csproj", "<Project Sdk=\"Uno.Sdk\"><PropertyGroup><UnoFeatures>Material;Toolkit</UnoFeatures></PropertyGroup></Project>");
        Write("Valid.xaml", $"<Page {Ns}>\n<Page.Resources><SolidColorBrush x:Key=\"StatusBrush\" Color=\"#18D96A\"/></Page.Resources>\n<Border CornerRadius=\"8\" Background=\"White\"><Grid/></Border>\n<Path Data=\"M0,0 L1,1\"/>\n</Page>");
        Write("Broken.xaml", $"<Page {Ns}>\n  <StackPanel>\n{Card}    </border>\n  </StackPanel>\n</Page>");

        var builtIn = new Linter(new LintOptions { Profile = Profile.Strict }).Run(_root);
        var withParser = new Linter(new LintOptions { Profile = Profile.Strict, XamlEngine = Engine }).Run(_root);

        Assert.Equal(1, builtIn.UnparsedXamlFiles);
        Assert.Equal(0, withParser.UnparsedXamlFiles);
        Assert.Equal(1, withParser.RepairedXamlFiles);

        // Valid file: identical findings whichever host runs HEX and CARD; TOKENTHEME does not double-report the
        // literal brush that HEX already reports; ICON still comes from the built-in host.
        static IEnumerable<string> Valid(LintResult r) => r.Findings.Where(f => f.File == "Valid.xaml").Select(f => $"{f.Rule.Name} L{f.Line} {f.Message}").OrderBy(s => s, StringComparer.Ordinal);
        Assert.Equal(Valid(builtIn), Valid(withParser));
        Assert.DoesNotContain(withParser.Findings, f => f.Rule == Rules.TokenTheme);
        Assert.Single(withParser.Findings, f => f.Rule == Rules.Icon);

        // Broken file: only the engine's rules ran, on the right lines.
        Assert.DoesNotContain(builtIn.Findings, f => f.File == "Broken.xaml");
        Assert.Equal(new[] { "CARD L3", "HEX L3" }, withParser.Findings.Where(f => f.File == "Broken.xaml").Select(f => $"{f.Rule.Name} L{f.Line}").OrderBy(s => s, StringComparer.Ordinal));
    }
}
