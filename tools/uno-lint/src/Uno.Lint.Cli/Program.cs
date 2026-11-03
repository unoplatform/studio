using Repl;
using Uno.Lint.Xaml;

namespace Uno.Lint.Cli;

internal static class Program
{
    private static readonly HashSet<string> HookOptions = new(StringComparer.Ordinal) { "--profile" };

    // Repl's own options, which every command accepts.
    private static readonly HashSet<string> GlobalOptions = new(StringComparer.OrdinalIgnoreCase) { "--help", "-h", "--json", "--xml", "--yaml", "--yml", "--markdown", "--human" };

    private static async Task<int> Main(string[] args)
    {
        var hookMode = args.Length > 0 && args[0] == "hook";
        if (hookMode)
        {
            NoteUnknownHookOptions(args);
        }

        using var xaml = new XamlLintEngine();
        var app = CoreReplApp.Create().WithDescription("uno-lint - semantic lint for Uno Platform XAML and C#");
        app.Options(options => UnoLintCommands.Configure(options, hookMode));
        app.MapModule(new UnoLintCommands(OutputFormats.Of(args), xaml));
        return await app.RunAsync(args);
    }

    /// <summary>An option Repl binds without a separate value: --profile=strict, --profile:strict, --json, --output:json.</summary>
    private static bool IsKnownInline(string arg)
    {
        var separator = arg.IndexOfAny(['=', ':']);
        return (separator > 0 && HookOptions.Contains(arg[..separator])) || GlobalOptions.Contains(arg) || arg.StartsWith("--output:", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Unknown hook options are ignored rather than failing every edit; say so where a developer can see it.</summary>
    private static void NoteUnknownHookOptions(string[] args)
    {
        var unknown = new List<string>();
        for (var i = 1; i < args.Length; i++)
        {
            var arg = args[i];
            if (HookOptions.Contains(arg))
            {
                i++; // its value
            }
            else if (arg.StartsWith('-') && !IsKnownInline(arg))
            {
                unknown.Add(arg);
            }
        }

        if (unknown.Count > 0)
        {
            Console.Error.WriteLine($"uno-lint: ignoring unknown hook option(s) {string.Join(" ", unknown)}; the hook command and the installed tool disagree.");
        }
    }
}
