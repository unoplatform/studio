using System.Text.Json.Serialization;

namespace Uno.Lint.Reports;

/// <summary>
/// The result of a lint run as one immutable value. The CLI renders it as text or JSON, and an MCP tool would return it
/// as is, so every surface reports the same shape. Severities, profiles and categories are lower-case strings, the
/// same words .editorconfig and --profile use.
/// </summary>
public sealed record LintReport(
    string Root,
    string Profile,
    ProjectStack Project,
    FileCounts Files,
    bool Gating,
    IReadOnlyList<RuleSummary> Rules,
    InfoCounts Info,
    IReadOnlyList<FindingReport> Findings,
    IReadOnlyList<FindingReport> SuppressedFindings,
    IReadOnlyList<string> Warnings)
{
    [JsonIgnore]
    public int ExitCode => Gating ? 1 : 0;

    public static LintReport From(LintResult result)
    {
        var findings = result.Findings.Select(FindingReport.From).ToList();
        var suppressed = result.Suppressed.Select(FindingReport.From).ToList();
        var counts = result.Findings.GroupBy(f => f.Rule).ToDictionary(g => g.Key, g => g.Count());
        var suppressedCounts = result.Suppressed.GroupBy(f => f.Rule).ToDictionary(g => g.Key, g => g.Count());

        var rules = Lint.Rules.All.Select(rule => new RuleSummary(
            rule.Id,
            rule.Name,
            Words.Of(result.ProjectSeverity(rule)),
            counts.GetValueOrDefault(rule),
            suppressedCounts.GetValueOrDefault(rule),
            rule.Title,
            Lint.Rules.FixFor(rule, result.Context))).ToList();

        var warnings = new List<string>();
        if (result.IgnoredPreambleKeys.Count > 0)
        {
            warnings.Add($"ignoring {string.Join(", ", result.IgnoredPreambleKeys)} severity above any [section] in .editorconfig; put it under [*] (an analyzer would not read it there either).");
        }

        foreach (var file in result.LineShiftedXamlFiles)
        {
            warnings.Add($"{file} needed markup repairs that changed its line count; its line numbers may be off until the markup is fixed.");
        }

        var context = result.Context;
        return new LintReport(
            result.Root,
            Words.Of(result.Profile),
            new ProjectStack(context.HasToolkit, context.HasExtensionsNavigation, context.HasMvux, context.HasUnoThemes),
            new FileCounts(result.FileCount, result.SkippedDevFiles, result.UnparsedXamlFiles, result.RepairedXamlFiles, result.PartialXamlFiles, result.LineShiftedXamlFiles.Count),
            result.HasGatingFindings,
            rules,
            new InfoCounts(result.Info.TokenReferences, result.Info.TokenKeys, result.Info.AdaptiveTriggers, result.Info.ResponsiveExtensions, result.Info.ControlTemplates, result.Info.WorkaroundMarkers),
            findings,
            suppressed,
            warnings);
    }
}

/// <summary>What the project references; the Toolkit and MVUX gates and the fix texts follow from it.</summary>
public sealed record ProjectStack(bool Toolkit, bool ExtensionsNavigation, bool Mvux, bool UnoThemes);

public sealed record FileCounts(int Linted, int SkippedDev, int UnparsedXaml, int RepairedXaml, int PartialXaml, int LineShiftedXaml);

/// <summary>One rule's outcome: its project-wide severity ("none" when the profile or a project gate turns it off), hits and suppressed hits.</summary>
public sealed record RuleSummary(string Id, string Name, string Severity, int Count, int Suppressed, string Title, string Fix);

public sealed record FindingReport(string Id, string Rule, string Category, string Severity, string File, int Line, string Message, string Fix)
{
    public static FindingReport From(Finding finding) =>
        new(finding.Rule.Id, finding.Rule.Name, Words.Of(finding.Rule.Category), Words.Of(finding.Severity), finding.File, finding.Line, finding.Message, finding.Fix);
}

public sealed record InfoCounts(int TokenRefs, int TokenKeys, int AdaptiveTriggers, int UtuResponsive, int ControlTemplates, int Workarounds);

/// <summary>The rule catalog, for <c>uno-lint rules</c>.</summary>
public sealed record RuleCatalog(IReadOnlyList<RuleInfo> Rules)
{
    public static RuleCatalog Create() => new(Lint.Rules.All.Select(RuleInfo.From).ToList());
}

/// <summary>
/// One rule as the catalog lists it. <see cref="Profile"/> is the lowest --profile value that enables it;
/// <see cref="Requires"/> names the project gate ("toolkit", "mvux") that must also be open, or is null.
/// </summary>
public sealed record RuleInfo(string Id, string Name, string Category, string Profile, string? Requires, string DefaultSeverity, string Title)
{
    public static RuleInfo From(RuleDescriptor rule) =>
        new(rule.Id, rule.Name, Words.Of(rule.Category), Words.Of(rule.MinimumProfile), rule.RequiresToolkit ? "toolkit" : rule.RequiresMvux ? "mvux" : null, Words.Of(rule.DefaultSeverity), rule.Title);
}

/// <summary>Enum values as the lower-case words .editorconfig and the command line use.</summary>
internal static class Words
{
    public static string Of<T>(T value) where T : struct, Enum => value.ToString().ToLowerInvariant();
}
