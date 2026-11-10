using System.Text.Json.Nodes;

namespace Uno.Lint.Hooks;

/// <summary>
/// Claude Code's PostToolUse payload for Edit, MultiEdit and Write: <c>tool_input.file_path</c>, and
/// <c>tool_response.structuredPatch</c> when the host sends it (it does for Edit and Write). Without a patch, the
/// edit's <c>new_string</c> (each one for MultiEdit) is located in the saved file.
/// </summary>
public sealed class ClaudeCodeHookAdapter : IHookAdapter
{
    public string Harness => "claude-code";

    public HookRequest? TryRead(JsonNode payload, Func<string, string?> readFile)
    {
        if (HookAdapters.Str(HookAdapters.Field(HookAdapters.Field(payload, "tool_input"), "file_path")) is not { Length: > 0 } rawPath || !FileKinds.IsSourceFile(rawPath))
        {
            return null;
        }

        var path = HookAdapters.Resolve(rawPath, payload);
        if (readFile(path) is not { } fullText || !TryGetAddedLines(payload, fullText, out var lines))
        {
            return null;
        }

        return new HookRequest(Harness, [new HookEdit(path, lines)]);
    }

    /// <summary>
    /// The 1-based lines the edit added to the saved file. Uses tool_response.structuredPatch when the host sends it,
    /// else falls back to locating the edit's text in the file.
    /// </summary>
    public static bool TryGetAddedLines(JsonNode payload, string fullText, out HashSet<int> lines)
    {
        lines = [];
        var input = HookAdapters.Field(payload, "tool_input");
        var response = HookAdapters.Field(payload, "tool_response");

        if (HookAdapters.Field(response, "structuredPatch") is JsonArray patch)
        {
            // A Write that created the file has an empty patch: every line is new.
            if (HookAdapters.Str(HookAdapters.Field(response, "type")) == "create")
            {
                lines = EditRange.AllLines(fullText);
                return true;
            }

            var hunks = patch.OfType<JsonObject>().Select(h => new PatchHunk(
                h["newStart"] is JsonValue start && start.TryGetValue<int>(out var s) ? s : 1,
                (h["lines"] as JsonArray)?.Select(HookAdapters.Str).OfType<string>().ToList() ?? []));
            lines = EditRange.FromPatch(hunks);
            return true;
        }

        // Write without a patch replaced the whole file.
        if (HookAdapters.Str(HookAdapters.Field(input, "content")) != null)
        {
            lines = EditRange.AllLines(fullText);
            return true;
        }

        var edits = HookAdapters.Field(input, "edits") is JsonArray multi ? multi.ToList() : new List<JsonNode?> { input };
        var located = false;
        foreach (var edit in edits.OfType<JsonObject>())
        {
            var replaceAll = edit["replace_all"] is JsonValue all && all.TryGetValue<bool>(out var b) && b;
            if (EditRange.TryLocate(fullText, HookAdapters.Str(edit["old_string"]) ?? string.Empty, HookAdapters.Str(edit["new_string"]) ?? string.Empty, replaceAll, out var found))
            {
                lines.UnionWith(found);
                located = true;
            }
        }

        return located;
    }
}
