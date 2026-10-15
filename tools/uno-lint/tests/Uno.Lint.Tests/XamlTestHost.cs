using System.Runtime.CompilerServices;
using Uno.Lint;
using Uno.Lint.Xaml;

namespace Uno.Lint.Tests;

/// <summary>
/// The XAML rules run inside Uno.XamlParser. Tests share one engine (it serializes calls) and register it as the
/// default so <see cref="Linter"/> runs pick it up, as the CLI does at startup.
/// </summary>
internal static class XamlTestHost
{
    public static readonly XamlParserEngine Engine = new();

    [ModuleInitializer]
    internal static void Register() => XamlEngines.DefaultFactory = () => Engine;

    /// <summary>Lints one XAML document the way the Linter would, with every rule the options and context enable.</summary>
    public static List<Finding> Lint(string xaml, string fileName, ProjectContext context, LintOptions options, bool includeSuppressed) =>
        Engine.Analyze(fileName, xaml, context, rule => Rules.Resolve(rule, options, context) != Severity.None)
            .Findings.Where(f => includeSuppressed || !f.Suppressed).ToList();
}
