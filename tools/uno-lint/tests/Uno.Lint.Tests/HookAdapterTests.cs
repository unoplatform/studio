using System.Text.Json.Nodes;
using Uno.Lint.Hooks;
using Xunit;

namespace Uno.Lint.Tests;

/// <summary>Each harness adapter must find the lines the edit added, and nothing else.</summary>
public class HookAdapterTests
{
    private static HashSet<int> Added(string payloadJson, string fullText)
    {
        Assert.True(ClaudeCodeHookAdapter.TryGetAddedLines(JsonNode.Parse(payloadJson)!, fullText, out var lines));
        return lines;
    }

    // ------------------------------------------------------------ Claude Code

    [Fact]
    public void Structured_patch_gives_the_added_lines_only()
    {
        // Shape captured from a real Claude Code PostToolUse payload for Edit.
        var payload = """
            {"tool_name":"Edit","tool_input":{"file_path":"f.xaml","old_string":"b","new_string":"B"},
             "tool_response":{"structuredPatch":[{"oldStart":1,"oldLines":3,"newStart":1,"newLines":3,"lines":[" a","-b","+B"," c"]}]}}
            """;
        Assert.Equal([2], Added(payload, "a\nB\nc\n"));
    }

    [Fact]
    public void Structured_patch_covers_every_hunk_of_a_replace_all()
    {
        var payload = """
            {"tool_input":{"file_path":"f.xaml","replace_all":true},
             "tool_response":{"structuredPatch":[
               {"newStart":2,"lines":[" x","-old","+new1","+new2"," y"]},
               {"newStart":20,"lines":["-old","+new1","+new2","\\ No newline at end of file"]}]}}
            """;
        Assert.Equal([3, 4, 20, 21], Added(payload, "").Order());
    }

    [Fact]
    public void Write_that_creates_a_file_adds_every_line()
    {
        var payload = """{"tool_name":"Write","tool_input":{"file_path":"f.xaml","content":"a\nb\nc"},"tool_response":{"type":"create","structuredPatch":[],"originalFile":null}}""";
        Assert.Equal([1, 2, 3], Added(payload, "a\nb\nc"));
    }

    [Fact]
    public void Write_that_rewrites_a_file_unchanged_adds_nothing()
    {
        var payload = """{"tool_name":"Write","tool_input":{"file_path":"f.xaml","content":"a"},"tool_response":{"type":"update","structuredPatch":[]}}""";
        Assert.Empty(Added(payload, "a"));
    }

    [Fact]
    public void Fallback_reports_the_new_copy_not_an_earlier_suppressed_original()
    {
        // The agent copies a suppressed block further down. Searching for new_string alone finds the original first.
        var full = "<StackPanel>\n<!-- uno-lint: allow hex - legacy -->\n<Border Background=\"#FF0000\"/>\n<TextBlock/>\n<Border Background=\"#FF0000\"/>\n</StackPanel>";
        var payload = new JsonObject
        {
            ["tool_input"] = new JsonObject
            {
                ["file_path"] = "f.xaml",
                ["old_string"] = "<TextBlock/>\n",
                ["new_string"] = "<TextBlock/>\n<Border Background=\"#FF0000\"/>\n",
            },
        };
        Assert.True(ClaudeCodeHookAdapter.TryGetAddedLines(payload, full, out var lines));
        Assert.Equal([5], lines);
    }

    [Fact]
    public void Fallback_handles_each_edit_of_a_multi_edit()
    {
        var payload = """{"tool_input":{"file_path":"f.xaml","edits":[{"old_string":"b","new_string":"B"},{"old_string":"d","new_string":"D"}]}}""";
        Assert.Equal([2, 4], Added(payload, "a\nB\nc\nD\n").Order());
    }

    [Fact]
    public void Claude_adapter_ignores_other_tools_and_files()
    {
        var adapter = new ClaudeCodeHookAdapter();
        Assert.Null(adapter.TryRead(JsonNode.Parse("""{"tool_name":"Bash","tool_input":{"command":"ls"}}""")!, _ => "x"));
        Assert.Null(adapter.TryRead(JsonNode.Parse("""{"tool_input":{"file_path":"notes.md","content":"x"}}""")!, _ => "x"));
        Assert.Null(adapter.TryRead(JsonNode.Parse("""{"tool_input":{"file_path":"/gone/Page.xaml","content":"x"}}""")!, _ => null));
    }

    // ------------------------------------------------------------ Codex

    private static readonly string Cwd = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "codex-project"));

    private static JsonNode CodexPayload(string patch) => new JsonObject
    {
        ["hook_event_name"] = "PostToolUse",
        ["cwd"] = Cwd,
        ["tool_name"] = "apply_patch",
        ["tool_input"] = new JsonObject { ["command"] = patch },
        ["tool_response"] = "Success. Updated the following files:",
    };

    [Fact]
    public void Codex_update_hunks_are_located_in_the_saved_file()
    {
        var patch = """
            *** Begin Patch
            *** Update File: Views/MainPage.xaml
            @@ <StackPanel>
               <TextBlock Text="title"/>
            -  <Border Background="Pink"/>
            +  <Border Background="Red"/>
            +  <Border Background="Blue"/>
               <TextBlock Text="footer"/>
            *** End Patch
            """;
        var saved = "<Page>\n<StackPanel>\n  <TextBlock Text=\"title\"/>\n  <Border Background=\"Red\"/>\n  <Border Background=\"Blue\"/>\n  <TextBlock Text=\"footer\"/>\n</StackPanel>\n</Page>";
        var files = new Dictionary<string, string> { [Path.Combine(Cwd, "Views", "MainPage.xaml")] = saved };

        var request = new CodexHookAdapter().TryRead(CodexPayload(patch), path => files.GetValueOrDefault(path));

        Assert.NotNull(request);
        Assert.Equal("codex", request.Harness);
        var edit = Assert.Single(request.Edits);
        Assert.Equal(Path.Combine(Cwd, "Views", "MainPage.xaml"), edit.FilePath);
        Assert.Equal([4, 5], edit.AddedLines.Order());
    }

    [Fact]
    public void Codex_patches_that_add_move_and_delete_files_are_read_file_by_file()
    {
        var patch = """
            *** Begin Patch
            *** Add File: Views/Detail.xaml
            +<Page>
            +  <Border Background="Red"/>
            +</Page>
            *** Update File: Old.cs
            *** Move to: Views/Detail.xaml.cs
            @@
             partial class Detail {
            +  void H(object sender, EventArgs e) { }
             }
            *** Delete File: Views/Gone.xaml
            *** Update File: README.md
            @@
            +docs
            *** End Patch
            """;
        var files = new Dictionary<string, string>
        {
            [Path.Combine(Cwd, "Views", "Detail.xaml")] = "<Page>\n  <Border Background=\"Red\"/>\n</Page>",
            [Path.Combine(Cwd, "Views", "Detail.xaml.cs")] = "partial class Detail {\n  void H(object sender, EventArgs e) { }\n}",
        };

        var request = new CodexHookAdapter().TryRead(CodexPayload(patch), path => files.GetValueOrDefault(path));

        Assert.NotNull(request);
        Assert.Equal(
            ["Detail.xaml: 1,2,3", "Detail.xaml.cs: 2"],
            request.Edits.Select(e => $"{Path.GetFileName(e.FilePath)}: {string.Join(",", e.AddedLines.Order())}"));
    }

    [Fact]
    public void Codex_patch_in_an_argument_array_is_found_too()
    {
        var payload = CodexPayload("unused");
        payload["tool_input"] = new JsonObject { ["command"] = new JsonArray("apply_patch", "*** Begin Patch\n*** Add File: A.xaml\n+<Page/>\n*** End Patch") };
        var request = new CodexHookAdapter().TryRead(payload, _ => "<Page/>");
        Assert.Equal([1], Assert.Single(request!.Edits).AddedLines);
    }

    [Fact]
    public void A_freeform_patch_or_a_payload_of_another_shape_is_read_without_throwing()
    {
        var payload = CodexPayload("unused");
        payload["tool_input"] = "*** Begin Patch\n*** Add File: A.xaml\n+<Page/>\n*** End Patch";
        Assert.Equal([1], Assert.Single(new CodexHookAdapter().TryRead(payload, _ => "<Page/>")!.Edits).AddedLines);

        foreach (var shape in new[] { "[1,2]", "\"text\"", """{"tool_input":["a"],"tool_response":"x"}""", """{"tool_input":{"file_path":"A.xaml","edits":"x"}}""" })
        {
            Assert.Null(HookAdapters.Read(JsonNode.Parse(shape)!, _ => "<Page/>"));
        }
    }

    [Fact]
    public void Codex_adapter_ignores_shell_commands()
    {
        var payload = JsonNode.Parse("""{"tool_name":"Bash","cwd":"/w","tool_input":{"command":"dotnet build"}}""")!;
        Assert.Null(new CodexHookAdapter().TryRead(payload, _ => "x"));
        Assert.Null(HookAdapters.Read(payload, _ => "x"));
    }
}
