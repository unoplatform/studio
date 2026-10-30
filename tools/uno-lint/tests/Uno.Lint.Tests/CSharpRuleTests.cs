using Xunit;
using static Uno.Lint.Tests.TestHost;

namespace Uno.Lint.Tests;

/// <summary>The C# rules, rule by rule: what each one reports and what it leaves alone.</summary>
public class CSharpRuleTests
{
    // ------------------------------------------------------------ CODEBEHIND

    [Fact]
    public void CodeBehind_flags_event_handlers_and_visibility_assignments()
    {
        var code = """

            public sealed partial class MainPage : Page
            {
                public MainPage() { InitializeComponent(); }
                private void OnClick(object sender, RoutedEventArgs e) { Details.Visibility = Visibility.Visible; }
                private void Helper(int a, int b) { }
            }
            """;
        var hits = LintCs(code).Where(x => x.Rule == Rules.CodeBehind).ToList();
        Assert.Equal(2, hits.Count);
        Assert.Contains(hits, h => h.Message == "handler OnClick" && h.Line == 5);
        Assert.Contains(hits, h => h.Message.StartsWith("Details.Visibility"));
    }

    [Fact]
    public void CodeBehind_ignores_comments_strings_and_non_code_behind_files()
    {
        var code = """
            public sealed partial class MainPage : Page
            {
                // void Fake(object sender, RoutedEventArgs e) { }
                private const string Doc = "void Fake(object sender, RoutedEventArgs e)";
            }
            """;
        Assert.DoesNotContain(LintCs(code), x => x.Rule == Rules.CodeBehind);

        var handlerInModel = "public class MainModel { void OnX(object sender, EventArgs e) { Foo.Visibility = Visibility.Collapsed; } }";
        Assert.DoesNotContain(LintCs(handlerInModel, "MainModel.cs"), x => x.Rule == Rules.CodeBehind);
    }

    [Fact]
    public void CodeBehind_is_strict_only()
    {
        var code = "public sealed partial class P : Page { void OnX(object sender, RoutedEventArgs e) { } }";
        Assert.DoesNotContain(LintCs(code, profile: Profile.Recommended), x => x.Rule == Rules.CodeBehind);
        Assert.Single(LintCs(code, profile: Profile.Strict), x => x.Rule == Rules.CodeBehind);
    }

    [Fact]
    public void CodeBehind_requires_an_args_typed_second_parameter()
    {
        var code = """
            partial class P : Page {
              void A(object sender, RoutedEventArgs e) { }
              void B(object s, TappedRoutedEventArgs args) { }
              void C(object sender, string name) { }
              void D(object sender) { }
              void E(object sender, RoutedEventArgs e, int extra) { }
            }
            """;
        var hits = LintCs(code).Where(f => f.Rule == Rules.CodeBehind).Select(f => f.Message).ToList();
        Assert.Equal(["handler A", "handler B"], hits);
    }

    [Fact]
    public void App_lifecycle_handlers_are_not_code_behind_findings()
    {
        var app = """
            public sealed partial class App : Application {
              protected override void OnLaunched(LaunchActivatedEventArgs args) { }
              private void OnSuspending(object sender, SuspendingEventArgs e) { }
              private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e) { }
            }
            """;
        Assert.DoesNotContain(LintCs(app, "App.xaml.cs"), f => f.Rule == Rules.CodeBehind);
        Assert.DoesNotContain(LintCs(app, "Shell/MyApp.xaml.cs"), f => f.Rule == Rules.CodeBehind);

        var page = "public sealed partial class MainPage : Page { private void OnSuspending(object sender, SuspendingEventArgs e) { } }";
        Assert.Single(LintCs(page), f => f.Rule == Rules.CodeBehind);
    }

    // ------------------------------------------------------------ OVERLAY

    [Fact]
    public void Overlay_flags_visibility_toggle_on_overlay_named_elements()
    {
        var code = "public sealed partial class P : Page { void Show() { FilterSheet.Visibility = Visibility.Visible; this.LoadingOverlay.Visibility = Visibility.Collapsed; } }";
        var hits = LintCs(code);
        Assert.Equal(2, hits.Count(x => x.Rule == Rules.Overlay));
        Assert.DoesNotContain(hits, x => x.Rule == Rules.CodeBehind);
    }

    [Fact]
    public void Overlay_does_not_treat_plain_panels_as_overlays()
    {
        var hits = LintCs("partial class P : Page { void X() { RootPanel.Visibility = Visibility.Collapsed; FilterDrawer.Visibility = Visibility.Visible; } }");
        Assert.Single(hits, f => f.Rule == Rules.Overlay && f.Message.StartsWith("FilterDrawer"));
        Assert.Single(hits, f => f.Rule == Rules.CodeBehind && f.Message.StartsWith("RootPanel"));
    }

    // ------------------------------------------------------------ RESPONSIVE

    [Fact]
    public void Responsive_flags_size_comparisons_in_code_behind_only()
    {
        var code = """
            public sealed partial class P : Page
            {
                void OnSizeChanged(object s, SizeChangedEventArgs e)
                {
                    if (e.NewSize.Width >= 800) { }
                    var wide = ActualWidth > 600;
                    var ok = Items.Count > 3;
                }
            }
            """;
        Assert.Equal(2, LintCs(code).Count(x => x.Rule == Rules.Responsive));
        Assert.DoesNotContain(LintCs(code, "MainModel.cs"), x => x.Rule == Rules.Responsive);
    }

    [Fact]
    public void Responsive_ignores_has_layout_run_checks_against_zero()
    {
        var code = """
            partial class P : Page {
              void OnSizeChanged(object sender, SizeChangedEventArgs e) {
                if (Spacer.ActualHeight > 0) { }
                if (0 < Spacer.ActualHeight) { }
                if (e.NewSize.Width >= 720) { }
              } }
            """;
        Assert.Single(LintCs(code), f => f.Rule == Rules.Responsive);
    }

    // ------------------------------------------------------------ BUILTIN

    [Fact]
    public void Builtin_flags_custom_control_shadowing_platform_control()
    {
        var hit = Assert.Single(LintCs("namespace App.Controls { public sealed partial class StarRating : UserControl { } }", "StarRating.xaml.cs"), x => x.Rule == Rules.Builtin);
        Assert.Contains("RatingControl", hit.Message);
    }

    [Fact]
    public void Builtin_skips_when_companion_xaml_already_uses_the_control_or_base_is_not_a_control()
    {
        Assert.DoesNotContain(LintCs("public sealed partial class StarRating : UserControl { }", "StarRating.xaml.cs", companionXaml: "<UserControl><RatingControl/></UserControl>"), x => x.Rule == Rules.Builtin);
        Assert.DoesNotContain(LintCs("public class RatingService : IDisposable { }", "RatingService.cs"), x => x.Rule == Rules.Builtin);
    }

    [Fact]
    public void Builtin_toolkit_suggestions_are_gated_on_toolkit()
    {
        var code = "public sealed partial class RecipeCard : ContentControl { }";
        Assert.Single(LintCs(code, "RecipeCard.cs", toolkit: true), x => x.Rule == Rules.Builtin);
        Assert.DoesNotContain(LintCs(code, "RecipeCard.cs", toolkit: false), x => x.Rule == Rules.Builtin);
        Assert.Single(LintCs("public sealed partial class StarRating : Control { }", "StarRating.cs", toolkit: false), x => x.Rule == Rules.Builtin);
    }

    [Theory]
    [InlineData("OperatingHoursView")]
    [InlineData("ParameterEditor")]
    [InlineData("DiscardBanner")]
    [InlineData("SpreadsheetGrid")]
    [InlineData("SwitcherooPanel")]
    public void Builtin_ignores_control_words_inside_other_words(string className)
    {
        Assert.DoesNotContain(LintCs($"public sealed partial class {className} : UserControl {{ }}", className + ".xaml.cs"), x => x.Rule == Rules.Builtin);
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
        var hit = Assert.Single(LintCs($"public sealed partial class {className} : UserControl {{ }}", className + ".xaml.cs"), x => x.Rule == Rules.Builtin);
        Assert.Contains(suggestion, hit.Message);
    }

    // ------------------------------------------------------------ HEX in code

    [Fact]
    public void Hex_flags_colors_built_in_code()
    {
        var code = """
            using Microsoft.UI;
            partial class MainPage : Page
            {
                void Paint()
                {
                    Title.Foreground = new SolidColorBrush(Colors.White);
                    var c = Color.FromArgb(255, 122, 103, 248);
                    var d = ColorHelper.FromArgb(255, 1, 2, 3);
                    var none = Colors.Transparent;
                    var fine = new SolidColorBrush((Color)Resources["PrimaryColor"]);
                }
            }
            """;
        var hits = LintCs(code, "Painter.cs").Where(f => f.Rule == Rules.Hex).Select(f => f.Message).ToList();
        Assert.Equal(["Colors.White in code", "Color.FromArgb(...) in code", "ColorHelper.FromArgb(...) in code"], hits);
    }

    [Theory]
    [InlineData("Palette.cs", 0)]
    [InlineData("Theme/AppColors.cs", 0)]
    [InlineData("DesignTokens.cs", 0)]
    [InlineData("ColorPickerViewModel.cs", 1)]
    public void Color_literals_in_code_follow_the_palette_file_exemption(string fileName, int expected)
    {
        var code = "static class T { public static readonly Color Brand = Color.FromArgb(255, 1, 2, 3); }";
        Assert.Equal(expected, LintCs(code, fileName).Count(f => f.Rule == Rules.Hex));
    }

    // ------------------------------------------------------------ fix text

    [Fact]
    public void Hex_fix_text_depends_on_whether_uno_themes_is_referenced()
    {
        var themed = Rules.FixFor(Rules.Hex, new ProjectContext(hasUnoThemes: true));
        var fluent = Rules.FixFor(Rules.Hex, ProjectContext.Empty);
        Assert.Contains("Uno.Themes", themed);
        Assert.Contains("SurfaceBrush", themed);
        Assert.DoesNotContain("Uno.Themes", fluent);
        Assert.DoesNotContain("SurfaceBrush", fluent);
        Assert.Contains("TextFillColorPrimaryBrush", fluent);
    }
}
