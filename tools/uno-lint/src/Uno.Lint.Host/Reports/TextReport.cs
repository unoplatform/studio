using System.Text;

namespace Uno.Lint.Reports;

/// <summary>The human-readable rendering of a <see cref="LintReport"/> and a <see cref="RuleCatalog"/>.</summary>
public static class TextReport
{
    /// <summary>Files listed per rule unless --top says otherwise.</summary>
    public const int DefaultTop = 12;

    /// <summary>Hits shown inline per file; the rest are counted as "+N more".</summary>
    private const int SamplesPerFile = 3;

    public static string Render(LintReport report, int top = DefaultTop)
    {
        var sb = new StringBuilder();
        var project = report.Project;
        var stack = string.Join(", ", new[]
        {
            project.Toolkit ? "toolkit" : "no toolkit",
            project.UnoThemes ? "uno.themes" : "fluent only",
            project.Mvux ? "mvux" : null,
        }.Where(s => s != null));
        var files = report.Files;
        sb.AppendLine($"uno-lint  {report.Root}");
        sb.Append($"profile={report.Profile}  {stack}  {files.Linted} files, {files.SkippedDev} dev/harness skipped, {files.UnparsedXaml} xaml unparsed");
        if (files.RepairedXaml > 0)
        {
            sb.Append($", {files.RepairedXaml} xaml repaired ({files.PartialXaml} partial, {files.LineShiftedXaml} with shifted lines)");
        }

        sb.AppendLine();
        sb.AppendLine();
        sb.AppendLine("Id       Rule         Count  Supp  Title");
        sb.AppendLine("-------  -----------  -----  ----  -----");
        foreach (var rule in report.Rules)
        {
            // A rule the profile or a project gate turns off shows "off"; one disabled only in some .editorconfig sections still shows its count.
            var count = rule.Severity == "none" && rule.Count == 0 ? "off" : rule.Count.ToString();
            sb.AppendLine($"{rule.Id}  {rule.Name,-11}  {count,5}  {(rule.Suppressed == 0 ? "" : rule.Suppressed.ToString()),4}  {rule.Title}");
        }

        var info = report.Info;
        sb.AppendLine($"{"",-7}  {"TOKEN",-11}  {info.TokenRefs,5}  {"",4}  info: refs to {info.TokenKeys} app-defined brush keys");
        sb.AppendLine($"{"",-7}  {"ADAPTIVE",-11}  {info.AdaptiveTriggers,5}  {"",4}  info: AdaptiveTrigger count (utu:Responsive: {info.UtuResponsive})");
        sb.AppendLine($"{"",-7}  {"TEMPLATE",-11}  {info.ControlTemplates,5}  {"",4}  info: app ControlTemplates");
        sb.AppendLine($"{"",-7}  {"WORKAROUND",-11}  {info.Workarounds,5}  {"",4}  info: WORKAROUND(...) markers");

        var suppressed = report.SuppressedFindings.Count;
        if (suppressed > 0)
        {
            var share = suppressed * 100 / Math.Max(1, suppressed + report.Findings.Count);
            sb.AppendLine($"Supp = hits covered by an allow comment ({suppressed} total, {share}% of all hits). Each needs a framework failure with a repro behind it.");
        }

        var byRule = report.Findings.ToLookup(f => f.Id);
        foreach (var rule in report.Rules)
        {
            var hits = byRule[rule.Id].ToList();
            if (hits.Count == 0)
            {
                continue;
            }

            var byFile = hits.GroupBy(h => h.File).OrderByDescending(g => g.Count()).ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase).ToList();
            sb.AppendLine();
            sb.AppendLine($"{rule.Name}: {hits.Count} hit(s) in {byFile.Count} file(s)");
            sb.AppendLine($"  fix: {rule.Fix}");
            foreach (var group in byFile.Take(top))
            {
                var inFile = group.ToList();
                var sample = string.Join("; ", inFile.Take(SamplesPerFile).Select(h => $"L{h.Line} {h.Message}"));
                var more = inFile.Count > SamplesPerFile ? $" (+{inFile.Count - SamplesPerFile} more)" : string.Empty;
                sb.AppendLine($"  {inFile.Count,4}  {group.Key}  {sample}{more}");
            }

            if (byFile.Count > top)
            {
                sb.AppendLine($"        +{byFile.Count - top} more file(s); --top raises the limit, --json lists every hit");
            }
        }

        foreach (var warning in report.Warnings)
        {
            sb.AppendLine();
            sb.AppendLine($"note: {warning}");
        }

        return sb.ToString();
    }

    public static string Render(RuleCatalog catalog)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Id       Rule         Category     Profile      Requires  Default     Title");
        sb.AppendLine("-------  -----------  -----------  -----------  --------  ----------  -----");
        foreach (var rule in catalog.Rules)
        {
            sb.AppendLine($"{rule.Id}  {rule.Name,-11}  {rule.Category,-11}  {rule.Profile,-11}  {rule.Requires ?? "",-8}  {rule.DefaultSeverity,-10}  {rule.Title}");
        }

        return sb.ToString();
    }
}
