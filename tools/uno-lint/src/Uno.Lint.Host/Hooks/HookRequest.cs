using System.Text.Json.Nodes;

namespace Uno.Lint.Hooks;

/// <summary>What an agent harness's post-edit hook payload says was written: which files, and which of their lines are new.</summary>
public sealed record HookRequest(string Harness, IReadOnlyList<HookEdit> Edits);

/// <summary>One edited file. <see cref="AddedLines"/> holds 1-based line numbers in the file as saved.</summary>
public sealed record HookEdit(string FilePath, IReadOnlySet<int> AddedLines);

/// <summary>
/// Reads one harness's PostToolUse payload. Adapters only describe the edit; linting and the reply are the same for
/// every harness (see <see cref="HookRunner"/>).
/// </summary>
public interface IHookAdapter
{
    string Harness { get; }

    /// <summary>
    /// Null when the payload is not this harness's file edit, or names no .xaml/.cs file. An edit whose added lines
    /// cannot be located in the saved file is left out: blaming lines the agent did not write is worse than silence.
    /// </summary>
    /// <param name="readFile">Reads a saved file, or returns null when it does not exist or cannot be read.</param>
    HookRequest? TryRead(JsonNode payload, Func<string, string?> readFile);
}

/// <summary>The adapters the hook tries, in order.</summary>
public static class HookAdapters
{
    public static IReadOnlyList<IHookAdapter> All { get; } = [new CodexHookAdapter(), new ClaudeCodeHookAdapter()];

    public static HookRequest? Read(JsonNode payload, Func<string, string?> readFile)
    {
        foreach (var adapter in All)
        {
            if (adapter.TryRead(payload, readFile) is { } request)
            {
                return request;
            }
        }

        return null;
    }

    internal static string? Str(JsonNode? node) => node is JsonValue value && value.TryGetValue<string>(out var s) ? s : null;

    /// <summary>A member of a JSON object; null when the node is missing or not an object, so a payload of another shape reads as absent rather than throwing.</summary>
    internal static JsonNode? Field(JsonNode? node, string name) => node is JsonObject obj ? obj[name] : null;

    /// <summary>An absolute path: relative ones are resolved against the payload's <c>cwd</c>, else the process's.</summary>
    internal static string Resolve(string path, JsonNode payload) =>
        Path.IsPathRooted(path) ? Path.GetFullPath(path) : Path.GetFullPath(Path.Combine(Str(Field(payload, "cwd")) ?? Environment.CurrentDirectory, path));
}
