using System.ComponentModel;
using System.Text;
using Repl;
using Repl.Parameters;
using Uno.Lint.Hooks;
using Uno.Lint.Reports;
using Uno.Lint.Xaml;

namespace Uno.Lint.Cli;

/// <summary>
/// The uno-lint command graph: <c>lint</c>, <c>rules</c> and <c>hook</c>. Help, option binding, structured output
/// (<c>--json</c>, <c>--output:yaml</c>...) and exit codes come from Repl; handlers return the host library's
/// immutable reports.
/// Exit codes: 0 no finding at warning or above; 1 at least one; 2 the run could not complete (bad option or path).
/// The hook always exits 0.
/// </summary>
public sealed class UnoLintCommands : IReplModule
{
    private readonly bool _structuredOutput;
    private readonly XamlLintEngine _xaml;

    /// <param name="structuredOutput">
    /// True when the invocation asks for a structured format (<see cref="OutputFormats.IsStructured"/>): handlers then
    /// return the report itself for Repl to serialize; otherwise its text rendering.
    /// </param>
    public UnoLintCommands(bool structuredOutput, XamlLintEngine xaml)
    {
        _structuredOutput = structuredOutput;
        _xaml = xaml;
    }

    public void Map(IReplMap map)
    {
        map.Map("lint", LintAsync)
            .WithDescription("Lint a project folder or one .xaml/.cs file. Exit code 1 when a finding is at warning or above. "
                + "Severities per rule and per path come from .editorconfig (dotnet_diagnostic.UNOL001.severity in any [glob] section). "
                + "An allow comment directly above a hit, reason required, suppresses it and is counted: <!-- uno-lint: allow hex - reason -->.");

        map.Map("rules", Rules)
            .WithDescription("List the rules: id, name, category, the lowest profile that enables each, the project gate it needs, and its default severity.");

        map.Map("hook", HookAsync)
            .AsProtocolPassthrough()
            .WithDescription("Agent post-edit hook (Claude Code Edit/MultiEdit/Write, Codex apply_patch): reads the payload on stdin and reports findings on the lines the edit added. Always exits 0.");
    }

    public static void Configure(ReplOptions options, bool hookMode)
    {
        options.Output.BannerEnabled = false;
        options.Interactive.InteractivePolicy = InteractivePolicy.Prevent;

        // A failed run is a usage problem, not a finding: keep 1 for findings only.
        options.ExitCodes.HandlerError = 2;
        options.ExitCodes.HandlerException = 2;
        options.ExitCodes.FrameworkError = 2;

        // The hook runs on every edit with whatever options the installed hook command passes: an option the tool does not
        // know (a newer hook snippet, an older tool) must not turn every edit into an error the agent sees.
        options.Parsing.AllowUnknownOptions = hookMode;
        if (hookMode)
        {
            options.ExitCodes.Resolver = _ => 0;
        }
    }

    private async Task<object> LintAsync(
        [Description("A project folder or a single .xaml/.cs file. Default: the current folder.")]
        [ReplArgument(Mode = ReplParameterMode.ArgumentOnly)]
        string? path = null,
        [Description("Rule set: recommended or strict. Default: uno_lint.profile in .editorconfig, else recommended.")]
        [ReplOption(Name = "profile", Mode = ReplParameterMode.OptionOnly)]
        ProfileName? profile = null,
        [Description("Files listed per rule in the text report.")]
        [ReplOption(Name = "top", Mode = ReplParameterMode.OptionOnly)]
        int top = TextReport.DefaultTop,
        [Description("Also lint Dev, Harness, Probe(s) and Test(s) folders.")]
        [ReplOption(Name = "include-dev", Mode = ReplParameterMode.OptionOnly)]
        bool includeDev = false,
        [Description("Force the Uno.Toolkit gate open (--toolkit) or closed (--no-toolkit) instead of detecting it.")]
        [ReplOption(Name = "toolkit", Mode = ReplParameterMode.OptionOnly, ReverseAliases = ["--no-toolkit"])]
        bool? toolkit = null,
        [Description("Detect the project context again instead of reading the cached one.")]
        [ReplOption(Name = "no-cache", Mode = ReplParameterMode.OptionOnly)]
        bool noCache = false,
        CancellationToken cancellationToken = default)
    {
        var target = path ?? ".";
        if (!File.Exists(target) && !Directory.Exists(target))
        {
            return Results.Validation($"Path not found: {target}");
        }

        if (File.Exists(target) && !FileKinds.IsSourceFile(target))
        {
            return Results.Validation($"Not a XAML or C# file: {target}");
        }

        if (top < 1)
        {
            return Results.Validation("--top must be at least 1.");
        }

        var options = new LintOptions { Profile = profile.ToProfile(), IncludeDev = includeDev, AssumeToolkit = toolkit, UseContextCache = !noCache };
        var result = await new Linter(options, _xaml).RunAsync(target, cancellationToken);
        var report = LintReport.From(result);
        return Results.Exit(report.ExitCode, _structuredOutput ? report : TextReport.Render(report, top));
    }

    private object Rules()
    {
        var catalog = RuleCatalog.Create();
        return _structuredOutput ? catalog : TextReport.Render(catalog);
    }

    private async Task<object> HookAsync(
        IReplIoContext io,
        [Description("Rule set for the hook. Default: uno_lint.profile in .editorconfig, else strict.")]
        [ReplOption(Name = "profile", Mode = ReplParameterMode.OptionOnly)]
        ProfileName? profile = null,
        CancellationToken cancellationToken = default)
    {
        // Harnesses send UTF-8; Console.In would decode it with the console code page on Windows and mangle any
        // non-ASCII path. A hosted session (tests) hands over its own reader.
        using var stdin = io.IsHostedSession ? null : new StreamReader(Console.OpenStandardInput(), new UTF8Encoding(false));
        await HookRunner.RunAsync(stdin ?? io.Input, io.Output, io.Error, HookRunner.Options(profile.ToProfile()), _xaml, cancellationToken);
        return Results.Exit(0, null);
    }
}

/// <summary>Which output an invocation asks for, decided from its arguments before Repl parses them.</summary>
public static class OutputFormats
{
    private static readonly HashSet<string> StructuredFlags = new(StringComparer.OrdinalIgnoreCase) { "--json", "--xml", "--yaml", "--yml", "--markdown" };

    /// <summary>True for --json and the other structured formats Repl renders, including --output:json and --output json.</summary>
    public static bool IsStructured(IReadOnlyList<string> args)
    {
        for (var i = 0; i < args.Count; i++)
        {
            var arg = args[i];
            if (StructuredFlags.Contains(arg))
            {
                return true;
            }

            var format = arg.StartsWith("--output:", StringComparison.OrdinalIgnoreCase) || arg.StartsWith("--output=", StringComparison.OrdinalIgnoreCase)
                ? arg["--output:".Length..]
                : string.Equals(arg, "--output", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Count ? args[i + 1] : null;
            if (format != null && !string.Equals(format, "human", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}

/// <summary>The --profile values, spelled as .editorconfig spells them (uno_lint.profile = strict).</summary>
#pragma warning disable IDE1006 // command-line words, not identifiers
public enum ProfileName
{
    recommended,
    strict,
}
#pragma warning restore IDE1006

internal static class ProfileNames
{
    public static Profile? ToProfile(this ProfileName? name) => name switch
    {
        ProfileName.recommended => Profile.Recommended,
        ProfileName.strict => Profile.Strict,
        _ => null,
    };
}
