using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Uno.XamlParser;
using Uno.XamlParser.Configuration;
using Uno.XamlParser.Stage0;
using Uno.XamlParser.Stage1;

namespace Uno.Lint.Xaml;

/// <summary>
/// Experimental XAML host on Uno.XamlParser. Runs Stage 0 (repairs on, so broken agent markup still yields a model)
/// and Stage 1 (fixers off, so values are read as written), then evaluates the rules it owns over the Stage-1 nodes.
/// Stage 2/3 need a type resolver, which a CLI or hook does not have, so they are not wired.
/// The pipeline holds per-run state in a singleton collector and is not thread-safe; calls are serialized.
/// </summary>
public sealed class XamlParserEngine : IXamlEngine
{
    private static readonly IReadOnlyCollection<RuleDescriptor> Owned = new[] { Rules.Hex, Rules.Card };

    private readonly object _gate = new object();
    private readonly XamlProcessingPipeline _pipeline;
    private readonly XamlSemanticCollectorVisitor _collector;

    public XamlParserEngine()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IXamlProcessingPolicy>(RepairMarkupOnly.Instance);
        services.AddXamlStreamPipeline().AddXamlProcessingPipeline();
        var provider = services.BuildServiceProvider();
        _pipeline = provider.GetRequiredService<XamlProcessingPipeline>();
        _collector = provider.GetRequiredService<XamlSemanticCollectorVisitor>();
    }

    public string Name => "Uno.XamlParser";

    public IReadOnlyCollection<RuleDescriptor> OwnedRules => Owned;

    public XamlEngineResult Analyze(string relativePath, string text, ProjectContext context, Func<RuleDescriptor, bool> isEnabled)
    {
        ParsedXaml doc;
        bool parsed, repaired, shifted, partial;
        List<string> repairCodes;

        lock (_gate)
        {
            var source = XamlStreamSources.FromString(text);
            var result = _pipeline.ProcessAsync(source).GetAwaiter().GetResult();

            // XML0001: the reader stopped even after repair. Elements read before the stop are complete and keep their
            // ancestry, so the rules still run on that prefix (reported as partial) rather than skipping the file.
            var stage0 = result.StreamResult.Diagnostics;
            partial = stage0.Any(d => d.Code == "XML0001");
            parsed = !partial || _collector.ElementStarts.Count > 0;
            repairCodes = stage0.Where(d => d.Code.StartsWith("XML1", StringComparison.Ordinal) || d.Code == "XML0001").Select(d => d.Code).Distinct().ToList();
            repaired = repairCodes.Count > 0;

            // The repaired text is only reachable through an internal interface; compare line counts to know whether
            // the parser's line numbers still point into the saved file. API gap to raise upstream.
            var repairedText = ReadRepairedText(source) ?? text;
            shifted = LineCount(repairedText) != LineCount(text);

            doc = new ParsedXaml(relativePath, Suppression.SplitLines(text), _collector.ElementStarts.ToArray(), _collector.TextNodes.ToArray());
        }

        var findings = new List<Finding>();
        if (parsed)
        {
            if (isEnabled(Rules.Hex) && !doc.IsPaletteFile && !doc.IsThemeOverrideFile)
            {
                findings.AddRange(ParserRules.FindHex(doc));
            }

            if (isEnabled(Rules.Card))
            {
                findings.AddRange(ParserRules.FindCards(doc));
            }
        }

        return new XamlEngineResult(parsed, repaired, shifted, findings, repairCodes, partial);
    }

    private static string? ReadRepairedText(IXamlStreamSource source) =>
        source.GetType().GetProperty("Text", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(source) as string;

    private static int LineCount(string text) => Suppression.SplitLines(text).Length;

    /// <summary>Repair markup so it parses (Stage 0); never rewrite values (Stage 1-3). Diagnostics are collected either way.</summary>
    private sealed class RepairMarkupOnly : IXamlProcessingPolicy
    {
        public static readonly RepairMarkupOnly Instance = new RepairMarkupOnly();

        public bool ShouldApplyStage0Fix(XamlDiagnostic diagnostic) => true;

        public bool ShouldApplyStage1Fix(XamlDiagnostic diagnostic) => false;

        public bool ShouldApplyStage2Edit(XamlPropertyEdit edit) => false;

        public bool ShouldApplyStage3Edit(XamlPropertyEdit edit) => false;
    }
}
