using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Uno.Lint;

namespace Uno.Lint.Cli;

internal static class Program
{
    private const string Usage = """
        uno-lint - semantic lint for Uno Platform XAML and C#

        Usage:
          uno-lint [<path>] [options]        Lint a project folder or a single file.
          uno-lint --hook [options]          Claude Code PostToolUse hook: reads the tool payload on stdin,
                                             reports findings inside the edited text, always exits 0.

        Options:
          --profile <recommended|strict>     Rule set. Default: recommended (or uno_lint.profile in .editorconfig).
          --json                             Machine-readable output.
          --top <n>                          Files listed per rule in the text report. Default: 12.
          --include-dev                      Also lint Dev/Harness/Probe/Tests paths.
          --toolkit / --no-toolkit           Force the Uno.Toolkit gate instead of detecting it.
          --list-rules                       Print the rule catalog.

        Exit codes: 0 clean, 1 findings at warning or above, 2 bad input.
        Severities per rule and per path come from .editorconfig (dotnet_diagnostic.UNOL001.severity = none, in any [glob] section).
        Suppress one hit with a comment directly above it (or above the element's start tag), reason required:
          <!-- uno-lint: allow hex - reason -->   // uno-lint: allow codebehind - reason   "allow all - reason" covers every rule there.
        Suppressions are counted per rule in the report; keep them for framework failures with a recorded repro.
        """;

    private static int Main(string[] args)
    {
        // XAML rules run inside Uno.XamlParser; the engine is created on first use, so C#-only runs never load it.
        XamlEngines.DefaultFactory = () => new Uno.Lint.Xaml.XamlParserEngine();
        var options = new LintOptions();
        string? path = null;
        var json = false;
        var hook = false;
        var listRules = false;
        var top = 12;
        var profileExplicit = false;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--json": json = true; break;
                case "--hook": hook = true; break;
                case "--list-rules": listRules = true; break;
                case "--include-dev": options.IncludeDev = true; break;
                case "--toolkit": options.AssumeToolkit = true; break;
                case "--no-toolkit": options.AssumeToolkit = false; break;
                case "--top":
                    if (++i >= args.Length || !int.TryParse(args[i], out top)) { return Fail("--top needs a number"); }
                    break;
                case "--profile":
                    if (++i >= args.Length || !Enum.TryParse<Profile>(args[i], ignoreCase: true, out var profile)) { return Fail("--profile needs recommended or strict"); }
                    options.Profile = profile;
                    profileExplicit = true;
                    break;
                case "-h":
                case "--help":
                    Console.WriteLine(Usage);
                    return 0;
                default:
                    if (args[i].StartsWith("-", StringComparison.Ordinal)) { return Fail($"unknown option {args[i]}"); }
                    path = args[i];
                    break;
            }
        }

        if (listRules)
        {
            foreach (var rule in Rules.All)
            {
                Console.WriteLine($"{rule.Id}  {rule.Name,-11} {rule.MinimumProfile,-12} {(rule.RequiresToolkit ? "toolkit" : ""),-8} {rule.Title}");
            }

            return 0;
        }

        if (hook)
        {
            return RunHook(options, profileExplicit);
        }

        path ??= ".";
        if (!File.Exists(path) && !Directory.Exists(path))
        {
            return Fail($"Path not found: {path}");
        }

        if (File.Exists(path) && !FileKinds.IsSourceFile(path))
        {
            return Fail($"Not a XAML or C# file: {path}");
        }

        // Configuration is resolved from the project root so a single file gets the same .editorconfig chain as a
        // directory run would give it (nested files included).
        var configRoot = Directory.Exists(path) ? path : Linter.FindProjectRoot(Path.GetDirectoryName(Path.GetFullPath(path))!);
        EditorConfig.Apply(configRoot, options, profileExplicit);
        if (options.EditorConfig != null && options.EditorConfig.IgnoredPreambleKeys.Count > 0 && !json)
        {
            Console.Error.WriteLine($"uno-lint: ignoring {string.Join(", ", options.EditorConfig.IgnoredPreambleKeys.Distinct())} severity above any [section] in .editorconfig; put it under [*] (a Roslyn analyzer would not read it there either).");
        }
        var result = new Linter(options).Run(path);

        if (json)
        {
            Console.WriteLine(ToJson(result, options));
        }
        else
        {
            Console.Write(ToText(result, options, top));
        }

        return result.HasGatingFindings ? 1 : 0;
    }

    // ---------------------------------------------------------------- hook mode

    private static int RunHook(LintOptions options, bool profileExplicit)
    {
        try
        {
            var payloadText = Console.In.ReadToEnd();
            var payload = JsonNode.Parse(payloadText);
            var input = payload?["tool_input"];
            var file = input?["file_path"]?.GetValue<string>();
            if (string.IsNullOrEmpty(file) || !(file!.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase) || file.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)))
            {
                return 0;
            }

            if (!File.Exists(file))
            {
                return 0;
            }

            // The edit is not located in the saved file (concurrent change, ambiguous text, a different path):
            // better to say nothing than to blame the agent for lines it did not write.
            var full = File.ReadAllText(file);
            if (!TryGetAddedLines(payload, full, out var addedLines) || addedLines.Count == 0)
            {
                return 0;
            }

            // Hooks default to the strict profile: the agent gets every hint, the human decides what to keep.
            if (!profileExplicit)
            {
                options.Profile = Profile.Strict;
            }

            EditorConfig.Apply(Linter.FindProjectRoot(Path.GetDirectoryName(Path.GetFullPath(file))!), options, profileExplicit);

            // Lint the whole file so context rules (ThemeDictionaries, NavigationBar, suppressions) see everything,
            // then keep only findings on the lines the edit added.
            var result = new Linter(options).RunSingleFile(file);
            var findings = result.Findings.Where(f => addedLines.Contains(f.Line)).ToList();
            var suppressed = result.Suppressed.Where(f => addedLines.Contains(f.Line)).ToList();

            if (findings.Count == 0)
            {
                return 0;
            }

            // Lead with the fix and keep the escape hatch out of the default message: in field transcripts an agent
            // offered "allow" on every hit took it on nearly half of them. Suppression stays documented in --help and
            // the README, and every suppression the agent does write is counted and reported.
            var leaf = Path.GetFileName(file);
            var rules = findings.Select(f => f.Rule).Distinct().ToList();
            var context = new StringBuilder();
            context.AppendLine($"uno-lint: this edit to {leaf} added {findings.Count} pattern(s) the platform already covers. Fix them before moving on:");
            foreach (var f in findings)
            {
                context.AppendLine($"- L{f.Line} {f.Rule.Name} ({f.Rule.Id}): {f.Message}");
            }

            foreach (var r in rules)
            {
                var fix = findings.First(f => f.Rule == r).Fix;
                context.AppendLine($"{r.Name} fix: {fix}");
            }

            if (suppressed.Count > 0)
            {
                context.AppendLine($"Note: this edit also added {suppressed.Count} allow comment(s) ({string.Join(", ", suppressed.Select(s => s.Rule.Name).Distinct())}). Suppressions are counted in the project report; each one needs a framework failure with a recorded repro behind it.");
            }

            var output = new JsonObject
            {
                ["systemMessage"] = $"uno-lint: {findings.Count} hit(s) in {leaf} ({string.Join(", ", rules.Select(r => r.Name))})" + (suppressed.Count > 0 ? $", {suppressed.Count} suppressed" : string.Empty),
                ["hookSpecificOutput"] = new JsonObject
                {
                    ["hookEventName"] = "PostToolUse",
                    ["additionalContext"] = context.ToString(),
                },
            };
            Console.WriteLine(output.ToJsonString());
        }
        catch
        {
            // A lint must never break an edit.
        }

        return 0;
    }

    /// <summary>
    /// The 1-based lines the edit added to the saved file. Uses tool_response.structuredPatch when the host sends it
    /// (Claude Code does, for Edit and Write), else falls back to locating the edit's text in the file.
    /// </summary>
    internal static bool TryGetAddedLines(JsonNode? payload, string fullText, out HashSet<int> lines)
    {
        lines = new HashSet<int>();
        var input = payload?["tool_input"];
        var response = payload?["tool_response"];

        if (response?["structuredPatch"] is JsonArray patch)
        {
            // A Write that created the file has an empty patch: every line is new.
            if (Str(response["type"]) == "create")
            {
                lines = EditRange.AllLines(fullText);
                return true;
            }

            var hunks = patch.OfType<JsonObject>().Select(h => new PatchHunk(
                h["newStart"] is JsonValue start && start.TryGetValue<int>(out var s) ? s : 1,
                (h["lines"] as JsonArray)?.Select(Str).OfType<string>().ToList() ?? new List<string>()));
            lines = EditRange.FromPatch(hunks);
            return true;
        }

        // Write without a patch replaced the whole file.
        if (Str(input?["content"]) != null)
        {
            lines = EditRange.AllLines(fullText);
            return true;
        }

        var edits = input?["edits"] is JsonArray multi ? multi.OfType<JsonObject>().Cast<JsonNode>().ToList() : new List<JsonNode> { input! };
        var located = false;
        foreach (var edit in edits)
        {
            var replaceAll = edit?["replace_all"] is JsonValue all && all.TryGetValue<bool>(out var b) && b;
            if (EditRange.TryLocate(fullText, Str(edit?["old_string"]) ?? string.Empty, Str(edit?["new_string"]) ?? string.Empty, replaceAll, out var found))
            {
                lines.UnionWith(found);
                located = true;
            }
        }

        return located;
    }

    private static string? Str(JsonNode? node) => node is JsonValue value && value.TryGetValue<string>(out var s) ? s : null;

    // ---------------------------------------------------------------- reports

    private static string ToJson(LintResult result, LintOptions options)
    {
        var counts = new JsonObject();
        var suppressedCounts = new JsonObject();
        foreach (var rule in Rules.All)
        {
            counts[rule.Name] = result.Findings.Count(f => f.Rule == rule);
            suppressedCounts[rule.Name] = result.SuppressedCount(rule);
        }

        var findings = new JsonArray();
        foreach (var f in result.Findings)
        {
            findings.Add(new JsonObject
            {
                ["id"] = f.Rule.Id,
                ["rule"] = f.Rule.Name,
                ["severity"] = f.Severity.ToString().ToLowerInvariant(),
                ["file"] = f.File,
                ["line"] = f.Line,
                ["message"] = f.Message,
                ["fix"] = f.Fix,
            });
        }

        var suppressed = new JsonArray();
        foreach (var f in result.Suppressed)
        {
            suppressed.Add(new JsonObject
            {
                ["id"] = f.Rule.Id,
                ["rule"] = f.Rule.Name,
                ["file"] = f.File,
                ["line"] = f.Line,
                ["message"] = f.Message,
            });
        }

        var obj = new JsonObject
        {
            ["root"] = result.Root,
            ["profile"] = options.Profile.ToString().ToLowerInvariant(),
            ["toolkit"] = result.Context.HasToolkit,
            ["extensionsNavigation"] = result.Context.HasExtensionsNavigation,
            ["mvux"] = result.Context.HasMvux,
            ["unoThemes"] = result.Context.HasUnoThemes,
            ["files"] = result.FileCount,
            ["skippedDev"] = result.SkippedDevFiles,
            ["unparsedXaml"] = result.UnparsedXamlFiles,
            ["xamlEngine"] = "Uno.XamlParser",
            ["repairedXaml"] = result.RepairedXamlFiles,
            ["lineShiftedXaml"] = result.LineShiftedXamlFiles,
            ["partialXaml"] = result.PartialXamlFiles,
            ["counts"] = counts,
            ["suppressed"] = suppressedCounts,
            ["suppressedFindings"] = suppressed,
            ["info"] = new JsonObject
            {
                ["tokenRefs"] = result.Info.TokenReferences,
                ["tokenKeys"] = result.Info.TokenKeys,
                ["adaptiveTriggers"] = result.Info.AdaptiveTriggers,
                ["utuResponsive"] = result.Info.ResponsiveExtensions,
                ["controlTemplates"] = result.Info.ControlTemplates,
                ["workarounds"] = result.Info.WorkaroundMarkers,
            },
            ["findings"] = findings,
        };

        return obj.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }

    private static string ToText(LintResult result, LintOptions options, int top)
    {
        var sb = new StringBuilder();
        var stack = string.Join(", ", new[]
        {
            result.Context.HasToolkit ? "toolkit" : "no toolkit",
            result.Context.HasUnoThemes ? "uno.themes" : "fluent only",
            result.Context.HasMvux ? "mvux" : null,
        }.Where(s => s != null));
        sb.AppendLine($"uno-lint  {result.Root}");
        sb.AppendLine($"profile={options.Profile.ToString().ToLowerInvariant()}  {stack}  {result.FileCount} files, {result.SkippedDevFiles} dev/harness skipped, {result.UnparsedXamlFiles} xaml unparsed" + (result.RepairedXamlFiles > 0 ? $", {result.RepairedXamlFiles} xaml repaired ({result.PartialXamlFiles} partial, {result.LineShiftedXamlFiles} with shifted lines)" : string.Empty));
        sb.AppendLine();
        sb.AppendLine("Id       Rule         Count  Supp  Title");
        sb.AppendLine("-------  -----------  -----  ----  -----");
        foreach (var rule in Rules.All)
        {
            // A rule disabled project-wide shows "off"; one disabled only in some .editorconfig sections still shows its count.
            var severity = Rules.Resolve(rule, options, result.Context, filePath: null);
            var hits = result.Findings.Count(f => f.Rule == rule);
            var count = severity == Severity.None && hits == 0 ? "off" : hits.ToString();
            var supp = result.SuppressedCount(rule);
            sb.AppendLine($"{rule.Id}  {rule.Name,-11}  {count,5}  {(supp == 0 ? "" : supp.ToString()),4}  {rule.Title}");
        }

        sb.AppendLine($"{"",-7}  {"TOKEN",-11}  {result.Info.TokenReferences,5}  {"",4}  info: refs to {result.Info.TokenKeys} app-defined brush keys");
        sb.AppendLine($"{"",-7}  {"ADAPTIVE",-11}  {result.Info.AdaptiveTriggers,5}  {"",4}  info: AdaptiveTrigger count (utu:Responsive: {result.Info.ResponsiveExtensions})");
        sb.AppendLine($"{"",-7}  {"TEMPLATE",-11}  {result.Info.ControlTemplates,5}  {"",4}  info: app ControlTemplates");
        sb.AppendLine($"{"",-7}  {"WORKAROUND",-11}  {result.Info.WorkaroundMarkers,5}  {"",4}  info: WORKAROUND(...) markers");
        if (result.Suppressed.Count > 0)
        {
            sb.AppendLine($"Supp = hits covered by an allow comment ({result.Suppressed.Count} total, {result.Suppressed.Count * 100 / Math.Max(1, result.Suppressed.Count + result.Findings.Count)}% of all hits). Each needs a framework failure with a repro behind it.");
        }

        foreach (var rule in Rules.All)
        {
            var hits = result.Findings.Where(f => f.Rule == rule).ToList();
            if (hits.Count == 0) { continue; }

            sb.AppendLine();
            sb.AppendLine($"{rule.Name} (top {top} files):");
            foreach (var group in hits.GroupBy(h => h.File).OrderByDescending(g => g.Count()).Take(top))
            {
                var sample = string.Join("; ", group.Take(3).Select(h => $"L{h.Line} {h.Message}"));
                sb.AppendLine($"  {group.Count(),4}  {group.Key}  {sample}");
            }
        }

        return sb.ToString();
    }

    private static int Fail(string message)
    {
        Console.Error.WriteLine($"uno-lint: {message}");
        Console.Error.WriteLine(Usage);
        return 2;
    }
}
