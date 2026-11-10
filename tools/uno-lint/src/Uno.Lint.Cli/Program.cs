using Repl;
using Uno.Lint.Xaml;

namespace Uno.Lint.Cli;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        var hookMode = CommandLine.IsHook(args);
        var prepared = CommandLine.Prepare(args, Console.Error);

        using var xaml = new XamlLintEngine();
        var app = CoreReplApp.Create().WithDescription("uno-lint - semantic lint for Uno Platform XAML and C#");
        app.Options(options => UnoLintCommands.Configure(options, hookMode));
        app.MapModule(new UnoLintCommands(OutputFormats.Of(prepared), xaml));
        return await app.RunAsync(prepared);
    }
}
