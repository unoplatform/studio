using System.Text.RegularExpressions;
using System.Xml;
using Uno.XamlParser;
using Uno.XamlParser.Configuration;
using Uno.XamlParser.Stage0;
using Uno.XamlParser.Stage1;

namespace Uno.Lint.Xaml;

/// <summary>
/// The uno-lint XAML rules for any host that runs Uno.XamlParser (uno-lint itself, Studio Live, Uno.AI). Three steps:
/// <list type="number">
/// <item><see cref="AddTo"/> registers the rules on the host's pipeline. They are Stage-1 fixers that only report, with
/// the rule id as the diagnostic code (UNOL001...).</item>
/// <item><see cref="IsWellFormed"/> tells the host whether to run the Stage-0 sanitizers. In Uno.XamlParser
/// 0.10.0-dev.74 a sanitizer rewrites valid markup whose element text contains ":/" (ms-appx:///..., http://...) and
/// the reader then stops; running sanitizers only on malformed text avoids it.</item>
/// <item><see cref="Collect"/> turns the result into uno-lint findings: rule gating, the HEX/TOKENTHEME hand-off,
/// suppression comments, line order and the info counters.</item>
/// </list>
/// A pipeline (one service provider) processes one document at a time, like the parser's own collector; see
/// <see cref="XamlLintEngine"/> for a host that does all three and serializes its calls.
/// </summary>
public static partial class UnoLintXaml
{
    public static void AddTo(XamlStreamPipelineOptions options)
    {
        options.VisitorTypes.Add(typeof(ElementTextCapture));
        options.SemanticFixerTypes.Add(typeof(DocumentTracker));
        options.SemanticFixerTypes.Add(typeof(HexRule));
        options.SemanticFixerTypes.Add(typeof(TokenThemeRule));
        options.SemanticFixerTypes.Add(typeof(IconRule));
        options.SemanticFixerTypes.Add(typeof(CardRule));
        options.SemanticFixerTypes.Add(typeof(BackBarRule));
        options.SemanticFixerTypes.Add(typeof(BindValueRule));
    }

    /// <summary>
    /// True when <paramref name="text"/> is well-formed XML. Document type declarations count as malformed: XAML has
    /// none, and refusing them keeps entity expansion out of every reader.
    /// </summary>
    public static bool IsWellFormed(string text)
    {
        try
        {
            using var reader = XmlReader.Create(new StringReader(text), new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
            while (reader.Read())
            {
            }

            return true;
        }
        catch (XmlException)
        {
            return false;
        }
    }

    /// <summary>
    /// The uno-lint findings for one processed file.
    /// </summary>
    /// <param name="result">The pipeline result for <paramref name="text"/>, with the rules registered by <see cref="AddTo"/>.</param>
    /// <param name="relativePath">Path used in findings and for file-name roles (palette, icons).</param>
    /// <param name="text">The file as saved; findings carry line numbers into it and suppression comments are read from it.</param>
    /// <param name="isEnabled">Whether a rule is enabled for this file after profile, configuration and project gates.</param>
    /// <param name="repairedText">The text after Stage-0 repairs, when the host can read it, to detect repairs that shifted lines.</param>
    public static XamlFileFindings Collect(XamlProcessingResult result, string relativePath, string text, Func<RuleDescriptor, bool> isEnabled, string? repairedText = null)
    {
        ArgumentNullException.ThrowIfNull(result);

        // XML0001: the reader stopped even after repair. Elements read before the stop keep their ancestry, so the
        // rules ran on that prefix (reported as partial) rather than the file being skipped.
        var stage0 = result.StreamResult.Diagnostics;
        var elements = result.SemanticResult?.Elements ?? [];
        var partial = stage0.Any(d => d.Code == "XML0001");
        var parsed = !partial || elements.Count > 0;
        var repairCodes = stage0.Where(d => d.Code.StartsWith("XML1", StringComparison.Ordinal) || d.Code == "XML0001").Select(d => d.Code).Distinct().ToList();
        var shifted = repairCodes.Count > 0 && repairedText != null && TextLines.Count(repairedText) != TextLines.Count(text);

        // Each attribute's element start, so a finding on an attribute can be suppressed above its element and HEX can
        // claim the element it was found on.
        var owners = new Dictionary<(int Line, int Column), (int Line, int Column)>();
        int adaptiveTriggers = 0, controlTemplates = 0;
        var definedBrushKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var element in elements)
        {
            var start = (element.LineInfo.LineNumber, element.LineInfo.LinePosition);
            foreach (var property in element.Properties)
            {
                owners[(property.LineInfo.LineNumber, property.LineInfo.LinePosition)] = start;
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

        var findings = new List<Finding>();
        if (parsed)
        {
            var diagnostics = (result.SemanticResult?.Diagnostics ?? []).Where(d => d.Code.StartsWith("UNOL", StringComparison.Ordinal)).ToList();
            (int, int) OwnerOf(XamlLineInfo at) => owners.TryGetValue((at.LineNumber, at.LinePosition), out var owner) ? owner : (at.LineNumber, at.LinePosition);

            // A literal single-themed brush is one problem: HEX reports it with a theme note, so TOKENTHEME steps back
            // on any element HEX already reported while HEX is enabled for this file.
            var hexElements = isEnabled(Rules.Hex)
                ? diagnostics.Where(d => d.Code == Rules.Hex.Id).Select(d => OwnerOf(d.LineInfo)).ToHashSet()
                : [];

            var lines = TextLines.Split(text);
            foreach (var diagnostic in diagnostics)
            {
                if (Rules.Find(diagnostic.Code) is not { } rule || !isEnabled(rule))
                {
                    continue;
                }

                var at = diagnostic.LineInfo;
                if (rule == Rules.TokenTheme && hexElements.Contains((at.LineNumber, at.LinePosition)))
                {
                    continue;
                }

                var ownerLine = OwnerOf(at).Item1;
                var suppressed = Suppression.IsSuppressed(lines, rule, at.LineNumber, ownerLine);
                findings.Add(new Finding(rule, relativePath, at.LineNumber, diagnostic.Message, suppressed));
            }
        }

        findings.Sort((a, b) => a.Line != b.Line ? a.Line.CompareTo(b.Line) : string.CompareOrdinal(a.Rule.Id, b.Rule.Id));

        var isPalette = FileKinds.IsPaletteFile(relativePath);
        var references = isPalette ? new List<string>() : BrushReference().Matches(text).Select(m => m.Groups[1].Value).ToList();
        var info = new XamlInfo(adaptiveTriggers, controlTemplates, ResponsiveExtension().Count(text), references.Count, definedBrushKeys, references.ToHashSet(StringComparer.Ordinal));

        return new XamlFileFindings(parsed, repairCodes.Count > 0, shifted, partial, findings, repairCodes, info);
    }

    internal static ValueTask<SemanticFixResult> None => ValueTask.FromResult(SemanticFixResult.None);

    internal static void Report(XamlSemanticFixerContext context, RuleDescriptor rule, string message, XamlLineInfo at) =>
        context.ReportDiagnostic(rule.Id, message, at, XamlDiagnosticSeverity.Warning);

    internal static bool ReferencesThemeResource(string value) => ThemeResourceReference().IsMatch(value);

    [GeneratedRegex(@"\{\s*ThemeResource\b")]
    private static partial Regex ThemeResourceReference();

    [GeneratedRegex(@"\{\s*utu:Responsive\b")]
    private static partial Regex ResponsiveExtension();

    [GeneratedRegex(@"\{(?:ThemeResource|StaticResource)\s+([A-Za-z0-9_.]+Brush)\}")]
    private static partial Regex BrushReference();
}

/// <summary>The uno-lint findings for one XAML file, and what the parser had to do to read it.</summary>
public sealed class XamlFileFindings
{
    public XamlFileFindings(bool parsed, bool repaired, bool linesShifted, bool partial, IReadOnlyList<Finding> findings, IReadOnlyList<string> repairCodes, XamlInfo info)
    {
        Parsed = parsed;
        Repaired = repaired;
        LinesShifted = linesShifted;
        Partial = partial;
        Findings = findings;
        RepairCodes = repairCodes;
        Info = info;
    }

    /// <summary>The parser produced a document model, possibly after repairing the markup.</summary>
    public bool Parsed { get; }

    /// <summary>The markup was malformed and the parser repaired it before the rules ran.</summary>
    public bool Repaired { get; }

    /// <summary>The repair changed the line count, so reported lines may not match the saved file.</summary>
    public bool LinesShifted { get; }

    /// <summary>Even after repair the reader stopped early; the rules ran on the elements read before the stop.</summary>
    public bool Partial { get; }

    /// <summary>Every finding of the enabled rules in line order, suppressed ones flagged as such.</summary>
    public IReadOnlyList<Finding> Findings { get; }

    /// <summary>Diagnostic codes of the repairs applied (e.g. XML1006 mismatched tags).</summary>
    public IReadOnlyList<string> RepairCodes { get; }

    public XamlInfo Info { get; }
}

/// <summary>The non-gating XAML counters for one file.</summary>
public sealed class XamlInfo
{
    public XamlInfo(int adaptiveTriggers, int controlTemplates, int responsiveExtensions, int tokenReferences, IReadOnlyCollection<string> definedBrushKeys, IReadOnlyCollection<string> referencedBrushKeys)
    {
        AdaptiveTriggers = adaptiveTriggers;
        ControlTemplates = controlTemplates;
        ResponsiveExtensions = responsiveExtensions;
        TokenReferences = tokenReferences;
        DefinedBrushKeys = definedBrushKeys;
        ReferencedBrushKeys = referencedBrushKeys;
    }

    public int AdaptiveTriggers { get; }
    public int ControlTemplates { get; }
    public int ResponsiveExtensions { get; }
    public int TokenReferences { get; }
    public IReadOnlyCollection<string> DefinedBrushKeys { get; }
    public IReadOnlyCollection<string> ReferencedBrushKeys { get; }
}
