using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Uno.XamlParser;
using Uno.XamlParser.Configuration;
using Uno.XamlParser.Stage0;
using Uno.XamlParser.Stage1;

namespace Uno.Lint.Xaml;

/// <summary>
/// The uno-lint XAML host on Uno.XamlParser. The rules themselves are Stage-1 fixers (<see cref="UnoLintXamlRules"/>);
/// this class runs the pipeline and turns their diagnostics into uno-lint findings, adding what the parser does not
/// do: per-file rule gating, the HEX/TOKENTHEME hand-off, suppression comments and the info counters.
/// Stage 0 repairs are on, so broken markup still yields a model; Stage 1 edits are off, so values are read as written.
/// The parser's own fixers are not registered. Stage 2/3 need a type resolver, which a CLI or hook does not have.
/// The pipeline keeps per-run state in singletons and is not thread-safe; calls are serialized.
/// </summary>
public sealed class XamlParserEngine : IXamlEngine
{
    private static readonly Regex ResponsiveExtension = new(@"\{\s*utu:Responsive\b", RegexOptions.Compiled);
    private static readonly Regex BrushReference = new(@"\{(?:ThemeResource|StaticResource)\s+([A-Za-z0-9_.]+Brush)\}", RegexOptions.Compiled);

    private readonly object _gate = new();
    private readonly Host _strict;
    private readonly Lazy<Host> _repairing;

    public XamlParserEngine()
    {
        // Two pipelines with the same rules. Well-formed XAML goes through the one without Stage-0 sanitizers: the
        // quote-repair sanitizer in 0.10.0-dev.74 rewrites valid markup whenever element text contains ":/"
        // (ms-appx:///..., http://...) and the reader then stops, and the policy does not stop it. Only XAML that
        // really is malformed is repaired.
        _strict = new Host(useSanitizers: false);
        _repairing = new Lazy<Host>(() => new Host(useSanitizers: true));
    }

    private sealed class Host
    {
        public Host(bool useSanitizers)
        {
            var services = new ServiceCollection();
            services.AddSingleton<IXamlProcessingPolicy>(RepairMarkupOnly.Instance);
            services.AddXamlStreamPipeline(options =>
            {
                options.UseDefaultSanitizers = useSanitizers;
                options.UseDefaultSemanticFixers = false;
                UnoLintXamlRules.AddTo(options);
            }).AddXamlProcessingPipeline();
            var provider = services.BuildServiceProvider();
            Pipeline = provider.GetRequiredService<XamlProcessingPipeline>();
            Collector = provider.GetRequiredService<XamlSemanticCollectorVisitor>();
        }

        public XamlProcessingPipeline Pipeline { get; }

        public XamlSemanticCollectorVisitor Collector { get; }
    }

    private static bool IsWellFormed(string text)
    {
        try
        {
            using var reader = System.Xml.XmlReader.Create(new StringReader(text), new System.Xml.XmlReaderSettings { DtdProcessing = System.Xml.DtdProcessing.Prohibit, XmlResolver = null });
            while (reader.Read())
            {
            }

            return true;
        }
        catch (System.Xml.XmlException)
        {
            return false;
        }
    }

    public string Name => "Uno.XamlParser";

    public XamlEngineResult Analyze(string relativePath, string text, ProjectContext context, Func<RuleDescriptor, bool> isEnabled)
    {
        IReadOnlyList<XamlDiagnostic> diagnostics;
        Dictionary<(int, int), int> attributeOwners;
        List<string> repairCodes;
        bool parsed, partial, shifted;
        int adaptiveTriggers, controlTemplates;
        HashSet<string> definedBrushKeys;

        lock (_gate)
        {
            var host = IsWellFormed(text) ? _strict : _repairing.Value;
            var source = XamlStreamSources.FromString(text, relativePath);
            var result = host.Pipeline.ProcessAsync(source).GetAwaiter().GetResult();

            // XML0001: the reader stopped even after repair. Elements read before the stop keep their ancestry, so the
            // rules ran on that prefix (reported as partial) rather than the file being skipped.
            var stage0 = result.StreamResult.Diagnostics;
            partial = stage0.Any(d => d.Code == "XML0001");
            parsed = !partial || host.Collector.ElementStarts.Count > 0;
            repairCodes = stage0.Where(d => d.Code.StartsWith("XML1", StringComparison.Ordinal) || d.Code == "XML0001").Select(d => d.Code).Distinct().ToList();

            // The repaired text is only reachable through an internal interface; compare line counts to know whether
            // the parser's line numbers still point into the saved file. API gap raised upstream.
            shifted = Suppression.SplitLines(ReadRepairedText(source) ?? text).Length != Suppression.SplitLines(text).Length;

            diagnostics = (result.SemanticResult?.Diagnostics ?? Array.Empty<XamlDiagnostic>())
                .Where(d => d.Code.StartsWith("UNOL", StringComparison.Ordinal)).ToList();

            // A finding on an attribute may be suppressed by a comment above its element's start tag.
            attributeOwners = new Dictionary<(int, int), int>();
            adaptiveTriggers = controlTemplates = 0;
            definedBrushKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var element in host.Collector.ElementStarts)
            {
                foreach (var property in element.Properties)
                {
                    attributeOwners[(property.LineInfo.LineNumber, property.LineInfo.LinePosition)] = element.LineInfo.LineNumber;
                }

                switch (element.Name.LocalName)
                {
                    case "AdaptiveTrigger": adaptiveTriggers++; break;
                    case "ControlTemplate": controlTemplates++; break;
                }

                if (XamlNodes.Key(element) is { } key && key.EndsWith("Brush", StringComparison.Ordinal))
                {
                    definedBrushKeys.Add(key);
                }
            }
        }

        var isPalette = FileKinds.IsPaletteFile(relativePath);
        var hexActive = isEnabled(Rules.Hex) && !isPalette && !FileKinds.IsThemeOverrideFile(relativePath);
        var lines = Suppression.SplitLines(text);
        var findings = new List<Finding>();

        if (parsed)
        {
            foreach (var diagnostic in diagnostics)
            {
                var literalBrush = diagnostic.Code == UnoLintXamlRules.TokenThemeLiteralCode;
                var rule = literalBrush ? Rules.TokenTheme : Rules.Find(diagnostic.Code);
                if (rule == null || !isEnabled(rule) || (literalBrush && hexActive))
                {
                    continue;
                }

                var line = diagnostic.LineInfo.LineNumber;
                var anchors = attributeOwners.TryGetValue((line, diagnostic.LineInfo.LinePosition), out var owner) ? new[] { line, owner } : new[] { line };
                findings.Add(new Finding(rule, relativePath, line, diagnostic.Message, Suppression.IsSuppressed(lines, rule, anchors)));
            }
        }

        var references = isPalette ? new List<string>() : BrushReference.Matches(text).Select(m => m.Groups[1].Value).ToList();
        var info = new XamlInfo(adaptiveTriggers, controlTemplates, ResponsiveExtension.Matches(text).Count, references.Count, definedBrushKeys, references.ToHashSet(StringComparer.Ordinal));

        return new XamlEngineResult(parsed, repairCodes.Count > 0, shifted, partial, findings, repairCodes, info);
    }

    private static string? ReadRepairedText(IXamlStreamSource source) =>
        source.GetType().GetProperty("Text", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(source) as string;

    /// <summary>Repair markup so it parses (Stage 0); never rewrite values (Stage 1-3). Diagnostics are collected either way.</summary>
    private sealed class RepairMarkupOnly : IXamlProcessingPolicy
    {
        public static readonly RepairMarkupOnly Instance = new();

        public bool ShouldApplyStage0Fix(XamlDiagnostic diagnostic) => true;

        public bool ShouldApplyStage1Fix(XamlDiagnostic diagnostic) => false;

        public bool ShouldApplyStage2Edit(XamlPropertyEdit edit) => false;

        public bool ShouldApplyStage3Edit(XamlPropertyEdit edit) => false;
    }
}
