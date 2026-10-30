using System.Text.Json.Nodes;
using Uno.Lint.Hooks;
using Uno.Lint.Xaml;
using Xunit;
using static Uno.Lint.Tests.TestHost;

namespace Uno.Lint.Tests;

/// <summary>The hook end to end in memory: payload in, the reply envelope (or nothing) out, never a failure.</summary>
public class HookRunnerTests : IDisposable
{
    private readonly TempProject _project = new();

    public HookRunnerTests()
    {
        _project.Write("App.csproj", "<Project Sdk=\"Uno.Sdk\"><PropertyGroup><UnoFeatures>Toolkit;Mvux</UnoFeatures></PropertyGroup></Project>");
    }

    public void Dispose() => _project.Dispose();

    private static (string Output, string Error) Run(string payload, XamlLintEngine? engine = null)
    {
        var output = new StringWriter();
        var error = new StringWriter();
        HookRunner.RunAsync(new StringReader(payload), output, error, HookRunner.Options() with { UseContextCache = false }, engine ?? Engine).GetAwaiter().GetResult();
        return (output.ToString(), error.ToString());
    }

    private static string EditPayload(string file, int newStart, params string[] addedLines) => new JsonObject
    {
        ["tool_name"] = "Edit",
        ["tool_input"] = new JsonObject { ["file_path"] = file, ["old_string"] = "x", ["new_string"] = "y" },
        ["tool_response"] = new JsonObject
        {
            ["structuredPatch"] = new JsonArray(new JsonObject
            {
                ["newStart"] = newStart,
                ["lines"] = new JsonArray(addedLines.Select(l => (JsonNode)JsonValue.Create("+" + l)).ToArray()),
            }),
        },
    }.ToJsonString();

    [Fact]
    public void Reports_findings_on_the_added_lines_in_the_post_tool_use_envelope()
    {
        var file = _project.Write("Views/MainPage.xaml", $"<Page {Ns}>\n  <Border Background=\"#111111\"/>\n  <Border CornerRadius=\"8\" Background=\"#FF0000\"><Grid/></Border>\n</Page>");

        var (output, error) = Run(EditPayload(file, 3, "  <Border CornerRadius=\"8\" Background=\"#FF0000\"><Grid/></Border>"));

        Assert.Empty(error);
        var reply = JsonNode.Parse(output)!;
        Assert.Equal("uno-lint: 2 hit(s) in MainPage.xaml (HEX, CARD)", reply["systemMessage"]!.GetValue<string>());
        Assert.Equal("PostToolUse", reply["hookSpecificOutput"]!["hookEventName"]!.GetValue<string>());
        var context = reply["hookSpecificOutput"]!["additionalContext"]!.GetValue<string>();
        Assert.Contains("- L3 HEX (UNOL001): literal #FF0000 on Border.Background", context);
        Assert.Contains("- L3 CARD (UNOL101): <Border> drawn as a card", context);
        Assert.Contains("HEX fix: ", context);
        Assert.DoesNotContain("#111111", context); // line 2 was not part of this edit
        Assert.DoesNotContain("\r", context);
        Assert.Single(output.Trim().Split('\n'));
    }

    [Fact]
    public void Leads_with_bugs_before_style_patterns()
    {
        var file = _project.Write("Views/Form.xaml", $"<Page {Ns}>\n  <TextBox Text=\"{{Binding Name.Value, Mode=TwoWay}}\" Foreground=\"Red\"/>\n</Page>");

        var context = JsonNode.Parse(Run(EditPayload(file, 2, "x")).Output)!["hookSpecificOutput"]!["additionalContext"]!.GetValue<string>();

        Assert.StartsWith("uno-lint: this edit to Form.xaml added 1 bug(s) and 1 pattern(s) the platform already covers.", context);
        Assert.True(context.IndexOf("BINDVALUE", StringComparison.Ordinal) < context.IndexOf("HEX", StringComparison.Ordinal));
    }

    [Fact]
    public void Notes_allow_comments_the_edit_added()
    {
        var file = _project.Write("Views/Chart.xaml", $"<Page {Ns}>\n  <!-- uno-lint: allow hex - chart series -->\n  <Border Background=\"#FF0000\"/>\n  <Border Background=\"#00FF00\"/>\n</Page>");

        var context = JsonNode.Parse(Run(EditPayload(file, 2, "a", "b", "c")).Output)!["hookSpecificOutput"]!["additionalContext"]!.GetValue<string>();

        Assert.Contains("this edit also added 1 allow comment(s) (HEX)", context);
    }

    [Fact]
    public void Says_nothing_for_clean_edits_other_tools_and_other_files()
    {
        var clean = _project.Write("Views/Clean.xaml", $"<Page {Ns}>\n  <Border Background=\"{{ThemeResource SurfaceBrush}}\"/>\n</Page>");
        _project.Write("notes.md", "# notes");

        Assert.Equal((string.Empty, string.Empty), Run(EditPayload(clean, 2, "x")));
        Assert.Equal((string.Empty, string.Empty), Run("""{"tool_name":"Bash","tool_input":{"command":"ls"}}"""));
        Assert.Equal((string.Empty, string.Empty), Run(EditPayload(_project.Path("notes.md"), 1, "x")));
        Assert.Equal((string.Empty, string.Empty), Run(EditPayload(_project.Path("Views/Missing.xaml"), 1, "x")));
        Assert.Equal((string.Empty, string.Empty), Run(""));
    }

    [Fact]
    public void A_malformed_payload_is_one_line_on_stderr_and_nothing_on_stdout()
    {
        var (output, error) = Run("{not json");
        Assert.Empty(output);
        Assert.StartsWith("uno-lint: the hook payload is not JSON", error);
    }

    [Fact]
    public void A_failure_inside_the_lint_is_one_line_on_stderr_and_nothing_on_stdout()
    {
        var file = _project.Write("Views/MainPage.xaml", $"<Page {Ns}><Border Background=\"#FF0000\"/></Page>");
        var broken = new XamlLintEngine();
        broken.Dispose();

        var (output, error) = Run(EditPayload(file, 1, "x"), broken);

        Assert.Empty(output);
        Assert.StartsWith("uno-lint: the hook failed, nothing reported: ObjectDisposedException", error);
    }

    [Fact]
    public void Reads_paths_with_non_ascii_characters()
    {
        var file = _project.Write("Vüés/Pagé.xaml", $"<Page {Ns}>\n  <Border Background=\"#FF0000\"/>\n</Page>");
        Assert.Contains("Pagé.xaml", JsonNode.Parse(Run(EditPayload(file, 2, "x")).Output)!["systemMessage"]!.GetValue<string>());
    }

    [Fact]
    public void Does_not_attribute_lines_in_a_file_whose_repair_shifted_them()
    {
        var file = _project.Write("Views/Broken.xaml", $"<Page {Ns}>\n  <Border Background=\"#FF0000\" /\n     junk>\n  <Border Background=\"#00FF00\"/>\n</Page>");

        var reply = JsonNode.Parse(Run(EditPayload(file, 4, "x")).Output)!;

        Assert.Equal("uno-lint: Broken.xaml is malformed XAML", reply["systemMessage"]!.GetValue<string>());
        Assert.Contains("Fix the markup, then save again.", reply["hookSpecificOutput"]!["additionalContext"]!.GetValue<string>());
    }

    [Fact]
    public void Lints_a_file_outside_any_project_without_walking_its_folder()
    {
        using var loose = new TempProject();
        var file = loose.Write("scratch.xaml", $"<Page {Ns}>\n  <Border Background=\"#FF0000\"/>\n</Page>");

        Assert.Contains("HEX", Run(EditPayload(file, 2, "x")).Output);
    }

    [Fact]
    public void Lints_every_file_a_codex_patch_touches()
    {
        _project.Write("Views/A.xaml", $"<Page {Ns}>\n  <Border Background=\"#FF0000\"/>\n</Page>");
        _project.Write("Views/B.xaml", $"<Page {Ns}>\n  <Border Background=\"#00FF00\"/>\n</Page>");
        var payload = new JsonObject
        {
            ["cwd"] = _project.Root,
            ["tool_name"] = "apply_patch",
            ["tool_input"] = new JsonObject
            {
                ["command"] = "*** Begin Patch\n*** Update File: Views/A.xaml\n@@\n-  <Border/>\n+  <Border Background=\"#FF0000\"/>\n*** Add File: Views/B.xaml\n+x\n*** End Patch",
            },
        };

        var context = JsonNode.Parse(Run(payload.ToJsonString()).Output)!["hookSpecificOutput"]!["additionalContext"]!.GetValue<string>();

        Assert.Contains("- A.xaml L2 HEX", context);
        Assert.Contains("- B.xaml L2 HEX", context);
    }

    [Fact]
    public void An_edit_that_only_adds_suppressed_hits_is_still_noted()
    {
        var file = _project.Write("Views/MainPage.xaml", $"<Page {Ns}>\n  <!-- uno-lint: allow hex - brand -->\n  <Border Background=\"#FF0000\"/>\n</Page>");

        var reply = JsonNode.Parse(Run(EditPayload(file, 2, "  <!-- uno-lint: allow hex - brand -->", "  <Border Background=\"#FF0000\"/>")).Output)!;

        Assert.Equal("uno-lint: 1 allow comment(s) added (HEX)", reply["systemMessage"]!.GetValue<string>());
        Assert.Contains("Note: this edit added 1 allow comment(s) (HEX)", reply["hookSpecificOutput"]!["additionalContext"]!.GetValue<string>());
    }

    [Fact]
    public void Files_with_the_same_name_are_told_apart_and_a_file_named_twice_is_linted_once()
    {
        _project.Write("Views/MainPage.xaml", $"<Page {Ns}>\n  <Border Background=\"#FF0000\"/>\n</Page>");
        _project.Write("Other/MainPage.xaml", $"<Page {Ns}>\n  <Border Background=\"Red\"/>\n</Page>");
        var payload = new JsonObject
        {
            ["cwd"] = _project.Root,
            ["tool_name"] = "apply_patch",
            ["tool_input"] = new JsonObject
            {
                ["command"] = "*** Begin Patch\n*** Add File: Other/MainPage.xaml\n+x\n*** Update File: Views/MainPage.xaml\n@@\n-  <Border/>\n+  <Border Background=\"#FF0000\"/>\n*** Update File: Views/MainPage.xaml\n@@\n-  <Border/>\n+  <Border Background=\"#FF0000\"/>\n*** End Patch",
            },
        };

        var context = JsonNode.Parse(Run(payload.ToJsonString()).Output)!["hookSpecificOutput"]!["additionalContext"]!.GetValue<string>();

        Assert.Contains($"- {Path.Combine("Other", "MainPage.xaml")} L2 HEX", context);
        Assert.Contains($"- {Path.Combine("Views", "MainPage.xaml")} L2 HEX", context);
        Assert.Equal(2, context.Split('\n').Count(l => l.StartsWith("- ", StringComparison.Ordinal)));
    }

    [Fact]
    public void Csharp_added_lines_follow_roslyn_line_breaks()
    {
        var separator = ((char)0x2028).ToString();
        var file = _project.Write("Views/Detail.cs", $"class A {{\n // s{separator}x\n void F(){{ var c = Colors.Red; }} }}");

        // The payload counts CR/LF lines: the Colors.Red line is its third; Roslyn reports it on line 4.
        Assert.Contains("HEX", Run(EditPayload(file, 3, " void F(){ var c = Colors.Red; } }")).Output);
        Assert.Empty(Run(EditPayload(file, 2, " // s")).Output);
    }
}
