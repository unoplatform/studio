using Xunit;
using static Uno.Lint.Tests.TestHost;

namespace Uno.Lint.Tests;

/// <summary>
/// Per-finding hints for the shapes agents kept after a generic fix text: which ramp style keeps a literal's size, and
/// what replaces each kind of code-behind handler.
/// </summary>
public class FixHintTests
{
    [Theory]
    [InlineData(UnoThemeKinds.Simple, "20", "TitleLarge (20)")]
    [InlineData(UnoThemeKinds.Simple, "13", "BodySmall (12)")] // a tie goes to the smaller size: a fix never grows text
    [InlineData(UnoThemeKinds.Material, "20", "TitleLarge (22)")]
    [InlineData(UnoThemeKinds.Material, "11", "LabelSmall (11)")]
    [InlineData(UnoThemeKinds.None, "16", "BodyTextBlockStyle (14)")]
    public void TypeRamp_names_the_nearest_style_in_the_project_theme(UnoThemeKinds theme, string size, string expected)
    {
        var context = new ProjectContext(themes: theme);
        var hit = Assert.Single(LintXaml($"<Page {Ns}><TextBlock FontSize=\"{size}\"/></Page>", context), f => f.Rule == Rules.TypeRamp);
        Assert.EndsWith($"; nearest ramp style {expected}", hit.Message);
    }

    [Fact]
    public void CodeBehind_names_what_replaces_each_handler_shape()
    {
        var code = """
            public sealed partial class WorkOrdersPage : Page
            {
                public WorkOrdersPage()
                {
                    InitializeComponent();
                    DataContextChanged += (_, _) => Attach();
                }

                private void OnDetailChanged(object? sender, PropertyChangedEventArgs e) { }
                private void OnAssigneeClick(object sender, ItemClickEventArgs e) { }
                private void OnBackRequested(object sender, BackRequestedEventArgs e) { }
                private void OnReassignCancel(object sender, RoutedEventArgs e) { }
            }
            """;
        var messages = CSharpAnalyzer.Analyze(new CSharpFile("WorkOrdersPage.xaml.cs", code, null), new ProjectContext(hasToolkit: true, hasMvux: true), _ => true)
            .Where(f => f.Rule == Rules.CodeBehind).OrderBy(f => f.Line).Select(f => f.Message).ToList();

        Assert.Equal(
        [
            "handler lambda on DataContextChanged: start loading in the model (a feed loads itself) and bind, instead of wiring the view model in code-behind",
            "handler OnDetailChanged: bind the property, or drive visual states from it with utu:VisualStateManagerExtensions.States",
            "handler OnAssigneeClick: utu:CommandExtensions.Command on the list passes the clicked item to a model command",
            "handler OnBackRequested: back belongs to navigation (utu:NavigationBar, or Navigation.Request=\"-\")",
            "handler OnReassignCancel",
        ], messages);
    }
}
