using System.Text.Json;
using Uno.Lint.Hooks;
using Uno.Lint.Reports;
using Uno.Lint.Xaml;

namespace Uno.Lint.Cli;

/// <summary>
/// The uno-lint command line: <c>lint</c>, <c>rules</c> and <c>hook</c>. Each command declares its options once, in
/// <see cref="Commands"/>; parsing and help both read that table, and CliTests check that help and the README list
/// every option.
/// <para>Options take <c>--name value</c>, <c>--name=value</c> or <c>--name:value</c>; flags take no value, so a path
/// after a flag is always the path. The last of <c>--json</c> and <c>--markdown</c> decides the output.</para>
/// <para>Exit codes: 0 no finding at warning or above; 1 at least one; 2 the run could not complete (bad option or path),
/// with one <c>uno-lint:</c> line on stderr. The hook always exits 0: an option it does not know, or a <c>--profile</c>
/// value it does not know, is noted on stderr and the hook runs with its defaults, so a hook command that disagrees
/// with the installed tool never turns every edit into an error the agent sees.</para>
/// </summary>
public static class UnoLintCli
{
    private const int Failed = 2;

    private static readonly Option Help = new("--help", null, "Show this help.", Alias: "-h");
    private static readonly Option Json = new("--json", null, "The report as JSON.");
    private static readonly Option Markdown = new("--markdown", null, "The text report in a Markdown code block.");
    private static readonly Option Toolkit = new("--toolkit", null, "Force the Uno.Toolkit gate open instead of detecting it.");
    private static readonly Option NoToolkit = new("--no-toolkit", null, "Force the Uno.Toolkit gate closed instead of detecting it.");

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    internal static readonly IReadOnlyList<Command> Commands =
    [
        new("lint", "[path] [options]",
            "Lint a project folder (default: the current one) or one .xaml/.cs file. Exit code 1 when a finding is at warning or above, 2 when the run cannot complete. "
            + "Severities per rule and per path come from .editorconfig (dotnet_diagnostic.UNOL001.severity in any [glob] section). "
            + "An allow comment directly above a hit, reason required, suppresses it and is counted: <!-- uno-lint: allow hex - reason -->.",
            [
                new("--profile", "recommended|strict", "Rule set. Default: uno_lint.profile in .editorconfig, else recommended."),
                new("--top", "n", $"Files listed per rule in the text report. Default: {TextReport.DefaultTop}."),
                new("--include-dev", null, "Also lint Dev, Harness, Probe(s) and Test(s) folders."),
                Toolkit,
                NoToolkit,
                new("--no-cache", null, "Detect the project context again instead of reading the cached one."),
                Json,
                Markdown,
                Help,
            ]),
        new("rules", "[options]",
            "List the rules: id, name, category, the lowest profile that enables each, the project gate it needs, and its default severity.",
            [Json, Markdown, Help]),
        new("hook", "[options] < payload",
            "Agent post-edit hook (Claude Code Edit/MultiEdit/Write, Codex apply_patch): reads the payload on stdin and reports findings on the lines the edit added. Always exits 0.",
            [
                new("--profile", "recommended|strict", "Rule set. Default: uno_lint.profile in .editorconfig, else strict."),
                Help,
            ]),
    ];

    /// <param name="input">Opened only by the hook, which reads its payload from it.</param>
    public static async Task<int> RunAsync(IReadOnlyList<string> args, Func<TextReader> input, TextWriter output, TextWriter error, XamlLintEngine xaml, CancellationToken cancellationToken = default)
    {
        if (args.Count == 0 || args[0] == Help.Name || args[0] == Help.Alias)
        {
            await output.WriteAsync(Usage()).ConfigureAwait(false);
            return 0;
        }

        var command = Commands.FirstOrDefault(c => c.Name == args[0]);
        if (command == null)
        {
            return Fail(error, $"unknown command {args[0]}; the commands are {string.Join(", ", Commands.Select(c => c.Name))}.");
        }

        var parsed = Parse(command, args.Skip(1).ToList());
        if (parsed.Has(Help))
        {
            await output.WriteAsync(UsageOf(command)).ConfigureAwait(false);
            return 0;
        }

        try
        {
            return command.Name switch
            {
                "lint" => await LintAsync(parsed, output, error, xaml, cancellationToken).ConfigureAwait(false),
                "rules" => await RulesAsync(parsed, output, error).ConfigureAwait(false),
                _ => await HookAsync(parsed, input, output, error, xaml, cancellationToken).ConfigureAwait(false),
            };
        }
        catch (Exception exception) when (command.Name != "hook" && exception is not OperationCanceledException)
        {
            return Fail(error, $"the run failed: {exception.GetType().Name}: {exception.Message}");
        }
    }

    private static async Task<int> LintAsync(Parsed parsed, TextWriter output, TextWriter error, XamlLintEngine xaml, CancellationToken cancellationToken)
    {
        if (parsed.Problem(allowArguments: 1) is { } problem)
        {
            return Fail(error, problem);
        }

        if (parsed.Positional.Count > 1)
        {
            return Fail(error, $"lint takes one path, not {parsed.Positional.Count}: {string.Join(" ", parsed.Positional)}");
        }

        var profile = parsed.Value("--profile");
        Profile? ruleSet = null;
        if (profile != null && (ruleSet = ParseProfile(profile)) == null)
        {
            return Fail(error, $"--profile must be recommended or strict, not '{profile}'.");
        }

        var top = TextReport.DefaultTop;
        if (parsed.Value("--top") is { } topValue && (!int.TryParse(topValue, out top) || top < 1))
        {
            return Fail(error, $"--top must be a whole number of at least 1, not '{topValue}'.");
        }

        var target = parsed.Positional.Count == 1 ? parsed.Positional[0] : ".";
        if (!File.Exists(target) && !Directory.Exists(target))
        {
            return Fail(error, $"Path not found: {target}");
        }

        if (File.Exists(target) && !FileKinds.IsSourceFile(target))
        {
            return Fail(error, $"Not a XAML or C# file: {target}");
        }

        var toolkit = parsed.Last(Toolkit, NoToolkit) is { } gate ? gate == Toolkit : (bool?)null;
        var options = new LintOptions { Profile = ruleSet, IncludeDev = parsed.Has("--include-dev"), AssumeToolkit = toolkit, UseContextCache = !parsed.Has("--no-cache") };
        var report = LintReport.From(await new Linter(options, xaml).RunAsync(target, cancellationToken).ConfigureAwait(false));
        await output.WriteLineAsync(Render(parsed, report, () => TextReport.Render(report, top))).ConfigureAwait(false);
        return report.ExitCode;
    }

    private static async Task<int> RulesAsync(Parsed parsed, TextWriter output, TextWriter error)
    {
        if (parsed.Problem(allowArguments: 0) is { } problem)
        {
            return Fail(error, problem);
        }

        var catalog = RuleCatalog.Create();
        await output.WriteLineAsync(Render(parsed, catalog, () => TextReport.Render(catalog))).ConfigureAwait(false);
        return 0;
    }

    private static async Task<int> HookAsync(Parsed parsed, Func<TextReader> input, TextWriter output, TextWriter error, XamlLintEngine xaml, CancellationToken cancellationToken)
    {
        var ignored = parsed.Unknown.Concat(parsed.Positional).ToList();
        if (ignored.Count > 0)
        {
            await error.WriteLineAsync($"uno-lint: ignoring unknown hook option(s) {string.Join(" ", ignored)}; the hook command and the installed tool disagree.").ConfigureAwait(false);
        }

        Profile? profile = null;
        if (parsed.Has("--profile") && (profile = ParseProfile(parsed.Value("--profile") ?? string.Empty)) == null)
        {
            await error.WriteLineAsync($"uno-lint: ignoring --profile '{parsed.Value("--profile")}' (expected recommended or strict); the hook runs with uno_lint.profile from .editorconfig, else strict.").ConfigureAwait(false);
        }

        using var stdin = input();
        await HookRunner.RunAsync(stdin, output, error, HookRunner.Options(profile), xaml, cancellationToken).ConfigureAwait(false);
        return 0;
    }

    private static int Fail(TextWriter error, string message)
    {
        error.WriteLine($"uno-lint: {message}");
        return Failed;
    }

    /// <summary>By name, in any case: Enum.TryParse would also take "1".</summary>
    private static Profile? ParseProfile(string value) => value.ToLowerInvariant() switch
    {
        "recommended" => Profile.Recommended,
        "strict" => Profile.Strict,
        _ => null,
    };

    /// <summary>The report as JSON, or its text rendering, fenced when Markdown was asked for.</summary>
    private static string Render<T>(Parsed parsed, T report, Func<string> text) => parsed.Last(Json, Markdown) switch
    {
        { } format when format == Json => JsonSerializer.Serialize(report, JsonOptions),
        { } format when format == Markdown => $"```text\n{text().TrimEnd()}\n```",
        _ => text(),
    };

    // ------------------------------------------------------------ parsing

    internal static Parsed Parse(Command command, IReadOnlyList<string> args)
    {
        var given = new List<(Option Option, string? Value)>();
        var positional = new List<string>();
        var unknown = new List<string>();
        var problems = new List<string>();
        for (var i = 0; i < args.Count; i++)
        {
            var arg = args[i];
            if (!arg.StartsWith('-') || arg.Length == 1)
            {
                positional.Add(arg);
                continue;
            }

            var separator = arg.IndexOfAny(['=', ':']);
            var name = separator > 0 ? arg[..separator] : arg;
            var option = command.Options.FirstOrDefault(o => o.Name == name || o.Alias == name);
            if (option == null)
            {
                unknown.Add(arg);
            }
            else if (option.Value == null)
            {
                if (separator > 0)
                {
                    problems.Add($"{option.Name} takes no value.");
                }

                given.Add((option, null));
            }
            else
            {
                // A value never starts with '-': `--profile --json` is a missing value, not the profile "--json".
                var value = separator > 0 ? arg[(separator + 1)..] : i + 1 < args.Count && !args[i + 1].StartsWith('-') ? args[++i] : null;
                given.Add((option, value));
            }
        }

        return new Parsed(command, given, positional, unknown, problems);
    }

    internal sealed record Option(string Name, string? Value, string Description, string? Alias = null);

    internal sealed record Command(string Name, string Arguments, string Description, IReadOnlyList<Option> Options);

    internal sealed record Parsed(Command Command, IReadOnlyList<(Option Option, string? Value)> Given, IReadOnlyList<string> Positional, IReadOnlyList<string> Unknown, IReadOnlyList<string> Problems)
    {
        public bool Has(Option option) => Given.Any(g => g.Option == option);

        public bool Has(string name) => Given.Any(g => g.Option.Name == name);

        /// <summary>The value of the last occurrence of the option; null when it is absent or has no value.</summary>
        public string? Value(string name) => Given.LastOrDefault(g => g.Option.Name == name).Value;

        /// <summary>Which of the options came last, or null when none was given.</summary>
        public Option? Last(params Option[] options) => Given.LastOrDefault(g => options.Contains(g.Option)).Option;

        /// <summary>The first thing wrong with the command line, for a command that takes up to <paramref name="allowArguments"/> paths.</summary>
        public string? Problem(int allowArguments)
        {
            if (Unknown.Count > 0)
            {
                return $"unknown option {Unknown[0]} for {Command.Name}; run uno-lint {Command.Name} --help for its options.";
            }

            if (allowArguments == 0 && Positional.Count > 0)
            {
                return $"unknown argument {Positional[0]}; {Command.Name} takes no path.";
            }

            if (Problems.Count > 0)
            {
                return Problems[0];
            }

            return Given.FirstOrDefault(g => g.Option.Value != null && string.IsNullOrEmpty(g.Value)).Option is { } missing
                ? $"{missing.Name} needs a value: {missing.Value}."
                : null;
        }
    }

    // ------------------------------------------------------------ help

    private static string Usage()
    {
        var width = Commands.Max(c => c.Name.Length);
        var lines = new List<string>
        {
            "uno-lint - semantic lint for Uno Platform XAML and C#",
            string.Empty,
            "Usage: uno-lint <command> [options]",
            string.Empty,
            "Commands:",
        };
        lines.AddRange(Commands.Select(c => $"  {c.Name.PadRight(width)}  {FirstSentence(c.Description)}"));
        lines.Add(string.Empty);
        lines.Add("Run uno-lint <command> --help for its options.");
        return string.Join(Environment.NewLine, lines) + Environment.NewLine;
    }

    private static string UsageOf(Command command)
    {
        var labels = command.Options.Select(o => (Label: (o.Alias == null ? o.Name : $"{o.Alias}, {o.Name}") + (o.Value == null ? string.Empty : $" <{o.Value}>"), o.Description)).ToList();
        var width = labels.Max(l => l.Label.Length);
        var lines = new List<string>
        {
            $"Usage: uno-lint {command.Name} {command.Arguments}",
            string.Empty,
            command.Description,
            string.Empty,
            "Options:",
        };
        lines.AddRange(labels.Select(l => $"  {l.Label.PadRight(width)}  {l.Description}"));
        return string.Join(Environment.NewLine, lines) + Environment.NewLine;
    }

    private static string FirstSentence(string text) => text.IndexOf(". ", StringComparison.Ordinal) is var end and > 0 ? text[..(end + 1)] : text;
}
