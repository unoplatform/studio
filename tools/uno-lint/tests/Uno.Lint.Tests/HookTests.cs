using System.Text.Json.Nodes;
using Uno.Lint;
using Uno.Lint.Cli;
using Xunit;

namespace Uno.Lint.Tests;

/// <summary>The hook must report the lines the edit added, and nothing else.</summary>
public class HookTests
{
    private static HashSet<int> Added(string payloadJson, string fullText)
    {
        Assert.True(Program.TryGetAddedLines(JsonNode.Parse(payloadJson), fullText, out var lines));
        return lines;
    }

    [Fact]
    public void Structured_patch_gives_the_added_lines_only()
    {
        // Shape captured from a real Claude Code PostToolUse payload for Edit.
        var payload = """
            {"tool_name":"Edit","tool_input":{"file_path":"f.xaml","old_string":"b","new_string":"B"},
             "tool_response":{"structuredPatch":[{"oldStart":1,"oldLines":3,"newStart":1,"newLines":3,"lines":[" a","-b","+B"," c"]}]}}
            """;
        Assert.Equal(new[] { 2 }, Added(payload, "a\nB\nc\n"));
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
        Assert.Equal(new[] { 3, 4, 20, 21 }, Added(payload, "").OrderBy(l => l));
    }

    [Fact]
    public void Write_that_creates_a_file_adds_every_line()
    {
        var payload = """{"tool_name":"Write","tool_input":{"file_path":"f.xaml","content":"a\nb\nc"},"tool_response":{"type":"create","structuredPatch":[],"originalFile":null}}""";
        Assert.Equal(new[] { 1, 2, 3 }, Added(payload, "a\nb\nc"));
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
        Assert.True(Program.TryGetAddedLines(payload, full, out var lines));
        Assert.Equal(new[] { 5 }, lines);
    }

    [Fact]
    public void Fallback_reports_nothing_when_the_new_text_is_ambiguous()
    {
        // new_string "/>" occurs on several lines: any line would be a guess.
        var full = "<StackPanel>\n<Border Background=\"#111111\"/>\n<TextBlock Text=\"hi\"/>\n</StackPanel>";
        Assert.False(EditRange.TryLocate(full, " Foo=\"1\"/>", "/>", replaceAll: false, out _));
    }

    [Fact]
    public void Fallback_skips_unchanged_context_lines_carried_in_new_string()
    {
        var full = "<Grid>\n<Border Background=\"#111111\"/>\n<Border Background=\"#222222\"/>\n</Grid>";
        Assert.True(EditRange.TryLocate(full,
            "<Border Background=\"#111111\"/>\n<TextBlock/>",
            "<Border Background=\"#111111\"/>\n<Border Background=\"#222222\"/>",
            replaceAll: false, out var lines));
        Assert.Equal(new[] { 3 }, lines);
    }

    [Fact]
    public void Fallback_handles_each_edit_of_a_multi_edit()
    {
        var full = "a\nB\nc\nD\n";
        var payload = """{"tool_input":{"file_path":"f.xaml","edits":[{"old_string":"b","new_string":"B"},{"old_string":"d","new_string":"D"}]}}""";
        Assert.Equal(new[] { 2, 4 }, Added(payload, full).OrderBy(l => l));
    }
}
