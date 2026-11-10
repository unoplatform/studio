namespace Uno.Lint.Cli;

/// <summary>
/// What Program.Main does to the arguments before Repl parses them:
/// <list type="bullet">
/// <item>A flag written before the path (<c>lint --no-cache App/Main.xaml</c>) gets an explicit value, because Repl
/// binds the next token as a flag's value and would read the path as a boolean.</item>
/// <item>In hook mode, an option the tool does not know, and a <c>--profile</c> value it does not know, are dropped
/// with one <c>uno-lint:</c> line on stderr, and the hook runs with its defaults: Repl would otherwise reject the
/// command before the hook runs, and a mistyped hook snippet would leave the linter silent on every edit.</item>
/// </list>
/// </summary>
public static class CommandLine
{
    /// <summary>The <c>lint</c> options bound to a bool: the canonical names in <see cref="UnoLintCommands"/>.</summary>
    internal static readonly IReadOnlySet<string> Flags = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "--include-dev", "--toolkit", "--no-cache" };

    private static readonly HashSet<string> HookOptions = new(StringComparer.Ordinal) { "--profile" };

    // Repl's own options, which every command accepts.
    private static readonly HashSet<string> GlobalOptions = new(StringComparer.OrdinalIgnoreCase) { "--help", "-h", "--json", "--xml", "--yaml", "--yml", "--markdown", "--human" };

    public static bool IsHook(IReadOnlyList<string> args) => args.Count > 0 && args[0] == "hook";

    /// <param name="error">Where hook-mode notes go; harnesses do not show an exit-0 hook's stderr to the agent.</param>
    public static string[] Prepare(IReadOnlyList<string> args, TextWriter error)
    {
        var prepared = IsHook(args) ? PrepareHook(args, error) : [.. args];
        for (var i = 0; i < prepared.Count; i++)
        {
            if (Flags.Contains(prepared[i]) && (i + 1 == prepared.Count || !bool.TryParse(prepared[i + 1], out _)))
            {
                prepared[i] += "=true";
            }
        }

        return [.. prepared];
    }

    private static List<string> PrepareHook(IReadOnlyList<string> args, TextWriter error)
    {
        var prepared = new List<string> { args[0] };
        var unknown = new List<string>();
        for (var i = 1; i < args.Count; i++)
        {
            var arg = args[i];
            if (HookOptions.Contains(arg))
            {
                var value = i + 1 < args.Count && !args[i + 1].StartsWith('-') ? args[++i] : string.Empty;
                AddProfile(prepared, value, error);
            }
            else if (InlineValue(arg) is { } inline)
            {
                AddProfile(prepared, inline, error);
            }
            else if (arg.StartsWith('-') && !GlobalOptions.Contains(arg) && !arg.StartsWith("--output:", StringComparison.OrdinalIgnoreCase))
            {
                unknown.Add(arg);
            }
            else
            {
                prepared.Add(arg);
            }
        }

        if (unknown.Count > 0)
        {
            error.WriteLine($"uno-lint: ignoring unknown hook option(s) {string.Join(" ", unknown)}; the hook command and the installed tool disagree.");
        }

        return prepared;
    }

    /// <summary>The value of --profile=x or --profile:x; null for any other argument.</summary>
    private static string? InlineValue(string arg)
    {
        var separator = arg.IndexOfAny(['=', ':']);
        return separator > 0 && HookOptions.Contains(arg[..separator]) ? arg[(separator + 1)..] : null;
    }

    private static void AddProfile(List<string> prepared, string value, TextWriter error)
    {
        // By name only: Enum.TryParse would also take "1".
        if (Enum.GetNames<ProfileName>().FirstOrDefault(name => string.Equals(name, value, StringComparison.OrdinalIgnoreCase)) is { } name)
        {
            prepared.Add($"--profile={name}");
        }
        else
        {
            error.WriteLine($"uno-lint: ignoring --profile '{value}' (expected {string.Join(" or ", Enum.GetNames<ProfileName>())}); the hook runs with uno_lint.profile from .editorconfig, else strict.");
        }
    }
}
