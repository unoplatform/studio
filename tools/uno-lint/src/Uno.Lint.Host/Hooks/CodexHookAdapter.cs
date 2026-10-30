using System.Text.Json.Nodes;

namespace Uno.Lint.Hooks;

/// <summary>
/// Codex's PostToolUse payload for file edits. Codex edits files with <c>apply_patch</c> (an <c>Edit|Write</c> matcher
/// matches it too) and reports <c>tool_name: "apply_patch"</c> with the patch in <c>tool_input.command</c>, paths
/// relative to the payload's <c>cwd</c>. One patch can touch several files:
/// <code>
/// *** Begin Patch
/// *** Update File: Views/MainPage.xaml
/// @@ optional context
///  context line
/// -removed line
/// +added line
/// *** Add File: Views/Detail.xaml
/// +every line of the new file
/// *** End Patch
/// </code>
/// Update hunks carry no line numbers, so each hunk's new text is located in the saved file.
/// </summary>
public sealed class CodexHookAdapter : IHookAdapter
{
    private const string BeginPatch = "*** Begin Patch";

    public string Harness => "codex";

    public HookRequest? TryRead(JsonNode payload, Func<string, string?> readFile)
    {
        // tool_name is "apply_patch" whatever matcher fired; the patch markers identify it too.
        if (PatchText(payload) is not { } patch)
        {
            return null;
        }

        var edits = new List<HookEdit>();
        foreach (var file in Parse(patch))
        {
            if (!FileKinds.IsSourceFile(file.Path))
            {
                continue;
            }

            var path = HookAdapters.Resolve(file.Path, payload);
            if (readFile(path) is not { } fullText)
            {
                continue;
            }

            if (file.IsNew)
            {
                edits.Add(new HookEdit(path, EditRange.AllLines(fullText)));
                continue;
            }

            var lines = new HashSet<int>();
            foreach (var hunk in file.Hunks)
            {
                if (EditRange.TryLocate(fullText, string.Join('\n', hunk.Old), string.Join('\n', hunk.New), replaceAll: false, out var found))
                {
                    lines.UnionWith(found);
                }
            }

            if (lines.Count > 0)
            {
                edits.Add(new HookEdit(path, lines));
            }
        }

        return edits.Count > 0 ? new HookRequest(Harness, edits) : null;
    }

    /// <summary>The patch from <c>tool_input.command</c> (a string, or an argument array whose element holds the patch), or <c>tool_input</c> itself when it is the patch.</summary>
    private static string? PatchText(JsonNode payload)
    {
        // tool_input is an object with a command, or, for a freeform apply_patch, the patch text itself.
        var input = HookAdapters.Field(payload, "tool_input");
        var command = input is JsonObject ? HookAdapters.Field(input, "command") : input;
        IEnumerable<string?> candidates = command is JsonArray parts ? parts.Select(HookAdapters.Str) : [HookAdapters.Str(command)];
        return candidates.FirstOrDefault(c => c != null && c.Contains(BeginPatch, StringComparison.Ordinal));
    }

    internal sealed class PatchFile(string path, bool isNew)
    {
        public string Path { get; set; } = path;
        public bool IsNew { get; } = isNew;
        public List<PatchHunkText> Hunks { get; } = [];
    }

    internal sealed class PatchHunkText
    {
        public List<string> Old { get; } = [];
        public List<string> New { get; } = [];
    }

    /// <summary>The files a patch adds or updates, with each update hunk's old and new text. Deleted files are left out.</summary>
    internal static List<PatchFile> Parse(string patch)
    {
        var files = new List<PatchFile>();
        PatchFile? current = null;
        PatchHunkText? hunk = null;
        var inPatch = false;

        foreach (var raw in TextLines.Split(patch))
        {
            if (!inPatch)
            {
                inPatch = raw.TrimEnd() == BeginPatch;
                continue;
            }

            if (raw.StartsWith("*** ", StringComparison.Ordinal))
            {
                hunk = null;
                if (raw.TrimEnd() == "*** End Patch")
                {
                    break;
                }

                if (Header(raw, "*** Add File: ") is { } added)
                {
                    current = new PatchFile(added, isNew: true);
                    files.Add(current);
                }
                else if (Header(raw, "*** Update File: ") is { } updated)
                {
                    current = new PatchFile(updated, isNew: false);
                    files.Add(current);
                }
                else if (Header(raw, "*** Move to: ") is { } moved && current != null)
                {
                    current.Path = moved;
                }
                else if (Header(raw, "*** Delete File: ") != null)
                {
                    current = null;
                }

                // "*** End of File" and unknown markers carry no text.
                continue;
            }

            if (current == null)
            {
                continue;
            }

            // A new file's lines are all added; they are counted from the saved file, not from the patch.
            if (current.IsNew)
            {
                continue;
            }

            if (raw.StartsWith("@@", StringComparison.Ordinal))
            {
                hunk = null;
                continue;
            }

            if (hunk == null)
            {
                hunk = new PatchHunkText();
                current.Hunks.Add(hunk);
            }

            // A blank context line may lose its leading space.
            var marker = raw.Length == 0 ? ' ' : raw[0];
            var text = raw.Length == 0 ? string.Empty : raw[1..];
            switch (marker)
            {
                case ' ':
                    hunk.Old.Add(text);
                    hunk.New.Add(text);
                    break;
                case '-':
                    hunk.Old.Add(text);
                    break;
                case '+':
                    hunk.New.Add(text);
                    break;
            }
        }

        return files;
    }

    private static string? Header(string line, string prefix) =>
        line.StartsWith(prefix, StringComparison.Ordinal) && line[prefix.Length..].Trim() is { Length: > 0 } path ? path : null;
}
