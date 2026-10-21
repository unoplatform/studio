using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Uno.Lint.Xaml;

namespace Uno.Lint.Hooks;

/// <summary>
/// The post-edit hook: payload in, one JSON reply out (or nothing), never a failed edit. Adapter, then linter, then
/// reply: an adapter per harness says which lines the edit added (<see cref="HookAdapters"/>), every edited file is
/// linted whole so context rules see everything, and only findings on added lines are reported. The reply uses the
/// PostToolUse shape both Claude Code and Codex accept: <c>systemMessage</c> for the human, and
/// <c>hookSpecificOutput.additionalContext</c> for the agent.
/// </summary>
public static class HookRunner
{
    private static readonly StringComparer PathComparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
    private static readonly StringComparison PathComparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    /// <summary>The hook's options: an explicit profile, else .editorconfig's, else strict. The agent gets every hint; the human decides what to keep.</summary>
    public static LintOptions Options(Profile? profile = null) => new() { Profile = profile, DefaultProfile = Profile.Strict };

    /// <param name="input">The payload, decoded as UTF-8 whatever the console code page.</param>
    /// <param name="error">Diagnostics for whoever debugs the hook. Harnesses do not show an exit-0 hook's stderr to the agent.</param>
    /// <param name="options">How each edited file is linted; see <see cref="Options"/> for the hook's defaults.</param>
    public static async Task RunAsync(TextReader input, TextWriter output, TextWriter error, LintOptions options, XamlLintEngine xaml, CancellationToken cancellationToken = default)
    {
        try
        {
            var text = await input.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            JsonNode? payload;
            try
            {
                payload = JsonNode.Parse(text);
            }
            catch (JsonException e)
            {
                await error.WriteLineAsync($"uno-lint: the hook payload is not JSON, nothing linted ({e.Message})").ConfigureAwait(false);
                return;
            }

            // Not a file edit this hook knows (another tool, another harness, a non-source file): say nothing.
            if (payload == null || HookAdapters.Read(payload, ReadFile) is not { } request)
            {
                return;
            }

            var linter = new Linter(options, xaml);
            var findings = new List<(string File, Finding Finding)>();
            var suppressed = new List<Finding>();
            var shifted = new List<string>();

            // A patch can name a file twice; it is linted once, with the lines of both hunks.
            var edits = request.Edits
                .GroupBy(e => Path.GetFullPath(e.FilePath), PathComparer)
                .Select(g => (Path: g.Key, Lines: g.SelectMany(e => e.AddedLines).ToHashSet()))
                .ToList();
            var labels = Labels(edits.Select(e => e.Path).ToList());
            foreach (var (path, lines) in edits)
            {
                var result = await linter.RunAsync(path, cancellationToken).ConfigureAwait(false);
                var label = labels[path];
                if (result.LineShiftedXamlFiles.Count > 0)
                {
                    // The repair moved lines, so a finding's line cannot be matched to the edit's lines.
                    shifted.Add(label);
                    continue;
                }

                // Payload lines are counted at CR and LF; C# findings carry Roslyn's lines, which also end at NEL, LS and PS.
                var added = !FileKinds.IsXaml(path) && ReadFile(path) is { } saved && TextLines.HasUnicodeLineBreaks(saved) ? TextLines.ToCSharpLines(saved, lines) : lines;
                findings.AddRange(result.Findings.Where(f => added.Contains(f.Line)).Select(f => (label, f)));
                suppressed.AddRange(result.Suppressed.Where(f => added.Contains(f.Line)));
            }

            if (findings.Count == 0 && suppressed.Count == 0 && shifted.Count == 0)
            {
                return;
            }

            await output.WriteLineAsync(Reply(findings, suppressed, shifted).ToJsonString()).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The harness gave up on the hook; nothing to report.
        }
        catch (Exception e)
        {
            // A lint must never break an edit, but a broken hook must not look like a clean edit either.
            await error.WriteLineAsync($"uno-lint: the hook failed, nothing reported: {e.GetType().Name}: {e.Message}").ConfigureAwait(false);
        }
    }

    /// <summary>
    /// How the reply names each edited file: its file name, or, when two edited files share one, its path from the
    /// folder all edited files share, so the agent can tell which file to fix.
    /// </summary>
    internal static Dictionary<string, string> Labels(IReadOnlyList<string> paths)
    {
        var labels = new Dictionary<string, string>(PathComparer);
        var clashes = paths.GroupBy(Path.GetFileName, PathComparer).Any(g => g.Count() > 1);
        var common = clashes ? CommonDirectory(paths) : null;
        foreach (var path in paths)
        {
            labels[path] = common != null ? Path.GetRelativePath(common, path) : Path.GetFileName(path);
        }

        return labels;
    }

    private static string? CommonDirectory(IReadOnlyList<string> paths)
    {
        var common = Path.GetDirectoryName(paths[0]);
        while (common != null && !paths.All(p => p.StartsWith(common.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, PathComparison)))
        {
            common = Path.GetDirectoryName(common);
        }

        return common;
    }

    private static string? ReadFile(string path)
    {
        try
        {
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    // Lead with the fix and keep the escape hatch out of the message: an agent offered "allow" on every hit tends to
    // take it. Suppression stays documented in --help and the README, and every allow comment written is counted.
    internal static JsonObject Reply(IReadOnlyList<(string File, Finding Finding)> findings, IReadOnlyList<Finding> suppressed, IReadOnlyList<string> shifted)
    {
        var files = findings.Select(f => f.File).Concat(shifted).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var oneFile = files.Count == 1;
        string Where((string File, Finding Finding) hit) => oneFile ? $"L{hit.Finding.Line}" : $"{hit.File} L{hit.Finding.Line}";

        var bugs = findings.Where(f => f.Finding.Rule.Category == RuleCategory.Correctness).ToList();
        var patterns = findings.Where(f => f.Finding.Rule.Category == RuleCategory.Style).ToList();
        var context = new StringBuilder();

        if (findings.Count > 0)
        {
            var parts = new List<string>();
            if (bugs.Count > 0)
            {
                parts.Add($"{bugs.Count} bug(s)");
            }

            if (patterns.Count > 0)
            {
                parts.Add($"{patterns.Count} pattern(s) the platform already covers");
            }

            context.AppendLine($"uno-lint: this edit to {string.Join(", ", files)} added {string.Join(" and ", parts)}. Fix them before moving on.");
        }

        if (bugs.Count > 0)
        {
            context.AppendLine("Bugs (the code does not do what it means):");
            foreach (var hit in bugs)
            {
                context.AppendLine($"- {Where(hit)} {hit.Finding.Rule.Name} ({hit.Finding.Rule.Id}): {hit.Finding.Message}");
            }
        }

        if (patterns.Count > 0)
        {
            context.AppendLine("Patterns to replace with the platform's own:");
            foreach (var hit in patterns)
            {
                context.AppendLine($"- {Where(hit)} {hit.Finding.Rule.Name} ({hit.Finding.Rule.Id}): {hit.Finding.Message}");
            }
        }

        foreach (var rule in findings.Select(f => f.Finding).GroupBy(f => f.Rule))
        {
            context.AppendLine($"{rule.Key.Name} fix: {rule.First().Fix}");
        }

        if (suppressed.Count > 0)
        {
            context.AppendLine($"Note: this edit {(findings.Count > 0 ? "also " : string.Empty)}added {suppressed.Count} allow comment(s) ({string.Join(", ", suppressed.Select(s => s.Rule.Name).Distinct())}). Suppressions are counted in the project report; each one needs a framework failure with a recorded repro behind it.");
        }

        foreach (var file in shifted)
        {
            context.AppendLine($"Note: {file} is malformed XAML; repairing it shifted its lines, so its findings cannot be matched to this edit. Fix the markup, then save again.");
        }

        var summary = findings.Count > 0
            ? $"uno-lint: {findings.Count} hit(s) in {string.Join(", ", files)} ({string.Join(", ", findings.Select(f => f.Finding.Rule.Name).Distinct())})" + (suppressed.Count > 0 ? $", {suppressed.Count} suppressed" : string.Empty)
            : shifted.Count > 0 ? $"uno-lint: {string.Join(", ", shifted)} is malformed XAML"
            : $"uno-lint: {suppressed.Count} allow comment(s) added ({string.Join(", ", suppressed.Select(s => s.Rule.Name).Distinct())})";

        return new JsonObject
        {
            ["systemMessage"] = summary,
            ["hookSpecificOutput"] = new JsonObject
            {
                ["hookEventName"] = "PostToolUse",
                ["additionalContext"] = context.ToString().ReplaceLineEndings("\n"),
            },
        };
    }
}
