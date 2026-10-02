using Uno.Lint;
using Xunit;

namespace Uno.Lint.Tests;

public class CSharpRuleTests
{
    private static List<Finding> Lint(string code, string fileName = "MainPage.xaml.cs", string? companionXaml = null, bool toolkit = true, Profile profile = Profile.Strict)
    {
        var file = new CSharpFile(fileName, code, companionXaml);
        var ctx = new ProjectContext { HasToolkit = toolkit };
        var options = new LintOptions { Profile = profile };
        return CSharpAnalyzer.Analyze(file, ctx, r => Rules.Resolve(r, options, ctx) != Severity.None).ToList();
    }

    // ------------------------------------------------------------ CODEBEHIND

    [Fact]
    public void CodeBehind_flags_event_handlers_and_visibility_assignments()
    {
        var code = @"
public sealed partial class MainPage : Page
{
    public MainPage() { InitializeComponent(); }
    private void OnClick(object sender, RoutedEventArgs e) { Details.Visibility = Visibility.Visible; }
    private void Helper(int a, int b) { }
}";
        var hits = Lint(code).Where(x => x.Rule == Rules.CodeBehind).ToList();
        Assert.Equal(2, hits.Count);
        Assert.Contains(hits, h => h.Message == "handler OnClick" && h.Line == 5);
        Assert.Contains(hits, h => h.Message.StartsWith("Details.Visibility"));
    }

    [Fact]
    public void CodeBehind_ignores_comments_strings_and_non_code_behind_files()
    {
        var code = @"
public sealed partial class MainPage : Page
{
    // void Fake(object sender, RoutedEventArgs e) { }
    private const string Doc = ""void Fake(object sender, RoutedEventArgs e)"";
}";
        Assert.Empty(Lint(code).Where(x => x.Rule == Rules.CodeBehind));

        var handlerInModel = "public class MainModel { void OnX(object sender, EventArgs e) { Foo.Visibility = Visibility.Collapsed; } }";
        Assert.Empty(Lint(handlerInModel, "MainModel.cs").Where(x => x.Rule == Rules.CodeBehind));
    }

    [Fact]
    public void CodeBehind_honours_suppression_and_recommended_profile()
    {
        var code = @"
public sealed partial class PlayerPage : Page
{
    // uno-lint: allow codebehind - MediaPlayerElement transport glue
    private void OnPositionChanged(object sender, RoutedEventArgs e) { }
}";
        Assert.Empty(Lint(code).Where(x => x.Rule == Rules.CodeBehind));

        var unsuppressed = "public sealed partial class P : Page { void OnX(object sender, RoutedEventArgs e) { } }";
        Assert.Empty(Lint(unsuppressed, profile: Profile.Recommended).Where(x => x.Rule == Rules.CodeBehind));
        Assert.Single(Lint(unsuppressed, profile: Profile.Strict).Where(x => x.Rule == Rules.CodeBehind));
    }

    // ------------------------------------------------------------ OVERLAY

    [Fact]
    public void Overlay_flags_visibility_toggle_on_overlay_named_elements()
    {
        var code = "public sealed partial class P : Page { void Show() { FilterSheet.Visibility = Visibility.Visible; this.LoadingOverlay.Visibility = Visibility.Collapsed; } }";
        var hits = Lint(code);
        Assert.Equal(2, hits.Count(x => x.Rule == Rules.Overlay));
        Assert.Empty(hits.Where(x => x.Rule == Rules.CodeBehind));
    }

    // ------------------------------------------------------------ RESPONSIVE

    [Fact]
    public void Responsive_flags_size_comparisons_in_code_behind_only()
    {
        var code = @"
public sealed partial class P : Page
{
    void OnSizeChanged(object s, SizeChangedEventArgs e)
    {
        if (e.NewSize.Width >= 800) { }
        var wide = ActualWidth > 600;
        var ok = Items.Count > 3;
    }
}";
        var hits = Lint(code).Where(x => x.Rule == Rules.Responsive).ToList();
        Assert.Equal(2, hits.Count);
        Assert.Empty(Lint(code, "MainModel.cs").Where(x => x.Rule == Rules.Responsive));
    }

    // ------------------------------------------------------------ BUILTIN

    [Fact]
    public void Builtin_flags_custom_control_shadowing_platform_control()
    {
        var code = "namespace App.Controls { public sealed partial class StarRating : UserControl { } }";
        var hit = Assert.Single(Lint(code, "StarRating.xaml.cs").Where(x => x.Rule == Rules.Builtin));
        Assert.Contains("RatingControl", hit.Message);
    }

    [Fact]
    public void Builtin_skips_when_companion_xaml_already_uses_the_control_or_base_is_not_a_control()
    {
        var code = "public sealed partial class StarRating : UserControl { }";
        Assert.Empty(Lint(code, "StarRating.xaml.cs", companionXaml: "<UserControl><RatingControl/></UserControl>").Where(x => x.Rule == Rules.Builtin));

        var service = "public class RatingService : IDisposable { }";
        Assert.Empty(Lint(service, "RatingService.cs").Where(x => x.Rule == Rules.Builtin));
    }

    [Fact]
    public void Builtin_toolkit_suggestions_are_gated_on_toolkit()
    {
        var code = "public sealed partial class RecipeCard : ContentControl { }";
        Assert.Single(Lint(code, "RecipeCard.cs", toolkit: true).Where(x => x.Rule == Rules.Builtin));
        Assert.Empty(Lint(code, "RecipeCard.cs", toolkit: false).Where(x => x.Rule == Rules.Builtin));

        var platform = "public sealed partial class StarRating : Control { }";
        Assert.Single(Lint(platform, "StarRating.cs", toolkit: false).Where(x => x.Rule == Rules.Builtin));
    }

    [Theory]
    [InlineData("OperatingHoursView")]
    [InlineData("ParameterEditor")]
    [InlineData("DiscardBanner")]
    [InlineData("SpreadsheetGrid")]
    [InlineData("SwitcherooPanel")]
    public void Builtin_ignores_control_words_inside_other_words(string className)
    {
        var code = $"public sealed partial class {className} : UserControl {{ }}";
        Assert.DoesNotContain(Lint(code, className + ".xaml.cs"), x => x.Rule == Rules.Builtin);
    }

    [Theory]
    [InlineData("StarRating", "RatingControl")]
    [InlineData("CalorieMeter", "ProgressBar")]
    [InlineData("MacroNutrientBar", "ProgressBar")]
    [InlineData("AppTopBar", "utu:NavigationBar")]
    [InlineData("Navbar", "utu:NavigationBar")]
    [InlineData("RecipeCard", "utu:CardContentControl")]
    [InlineData("FilterChips", "utu:Chip")]
    [InlineData("BottomSheet", "Flyout")]
    [InlineData("UIToggle", "ToggleSwitch")]
    [InlineData("BusyIndicator", "ProgressRing")]
    public void Builtin_matches_control_words(string className, string suggestion)
    {
        var code = $"public sealed partial class {className} : UserControl {{ }}";
        var hit = Assert.Single(Lint(code, className + ".xaml.cs"), x => x.Rule == Rules.Builtin);
        Assert.Contains(suggestion, hit.Message);
    }
}
