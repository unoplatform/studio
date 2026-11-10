using System.Text;
using Uno.Lint.Xaml;

namespace Uno.Lint.Cli;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        using var xaml = new XamlLintEngine();

        // Harnesses send UTF-8; Console.In would decode it with the console code page on Windows and mangle any
        // non-ASCII path.
        return await UnoLintCli.RunAsync(args, () => new StreamReader(Console.OpenStandardInput(), new UTF8Encoding(false)), Console.Out, Console.Error, xaml);
    }
}
