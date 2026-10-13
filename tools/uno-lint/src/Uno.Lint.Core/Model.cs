namespace Uno.Lint;

public enum Severity
{
    None,
    Info,
    Suggestion,
    Warning,
    Error,
}

/// <summary>Which rule set is enabled by default. Mirrors flutter_lints "core" vs "recommended".</summary>
public enum Profile
{
    /// <summary>Rules almost every Uno team accepts: colors, theme-awareness, icons, shadowed controls, breakpoints, binding traps.</summary>
    Recommended,
    /// <summary>Recommended plus the MVUX + Extensions.Navigation + Toolkit architectural rules.</summary>
    Strict,
}

public sealed class RuleDescriptor
{
    public RuleDescriptor(string id, string name, string title, string fix, Profile minimumProfile, bool requiresToolkit = false, bool requiresMvux = false, Severity defaultSeverity = Severity.Warning)
    {
        Id = id;
        Name = name;
        Title = title;
        Fix = fix;
        MinimumProfile = minimumProfile;
        RequiresToolkit = requiresToolkit;
        RequiresMvux = requiresMvux;
        DefaultSeverity = defaultSeverity;
    }

    /// <summary>Severity when the rule is enabled and nothing overrides it. Warning gates; Suggestion is reported but never fails a run.</summary>
    public Severity DefaultSeverity { get; }

    /// <summary>Diagnostic id, e.g. UNOL001. Used by .editorconfig overrides.</summary>
    public string Id { get; }

    /// <summary>Short upper-case name used in reports and in suppression comments (lower-cased).</summary>
    public string Name { get; }

    public string Title { get; }

    /// <summary>Default fix text. <see cref="Rules.FixFor"/> adapts it to the project (theme in use, Toolkit present).</summary>
    public string Fix { get; }

    public Profile MinimumProfile { get; }

    /// <summary>The rule's suggested replacement is a Uno.Toolkit control; skipped when the project has no Toolkit reference.</summary>
    public bool RequiresToolkit { get; }

    /// <summary>The rule describes an MVUX trap; skipped when the project does not use MVUX.</summary>
    public bool RequiresMvux { get; }

    public string SuppressionKeyword => Name.ToLowerInvariant();
}

public sealed class Finding
{
    public Finding(RuleDescriptor rule, string file, int line, string message, bool suppressed = false)
    {
        Rule = rule;
        File = file;
        Line = line;
        Message = message;
        Suppressed = suppressed;
        Fix = rule.Fix;
    }

    public RuleDescriptor Rule { get; }

    /// <summary>Path relative to the lint root (or the file name in single-file mode).</summary>
    public string File { get; }

    public int Line { get; }

    public string Message { get; }

    /// <summary>True when an "allow" comment covers this hit. Suppressed findings are counted, not reported as gating.</summary>
    public bool Suppressed { get; }

    /// <summary>Fix text adapted to the project; defaults to the rule's text.</summary>
    public string Fix { get; set; }

    public Severity Severity { get; set; } = Severity.Warning;

    public override string ToString() => $"{File}({Line}): {Rule.Id} {Rule.Name}: {Message}";
}

/// <summary>Non-gating counts reported alongside findings.</summary>
public sealed class InfoStats
{
    public int TokenReferences { get; set; }
    public int TokenKeys { get; set; }
    public int AdaptiveTriggers { get; set; }
    public int ResponsiveExtensions { get; set; }
    public int ControlTemplates { get; set; }
    public int WorkaroundMarkers { get; set; }
}

public sealed class LintResult
{
    public string Root { get; set; } = string.Empty;
    public int FileCount { get; set; }
    public int SkippedDevFiles { get; set; }
    public int UnparsedXamlFiles { get; set; }
    public ProjectContext Context { get; set; } = new ProjectContext();

    /// <summary>Active findings, after profile, .editorconfig and project gating; never includes suppressed hits.</summary>
    public List<Finding> Findings { get; } = new List<Finding>();

    /// <summary>
    /// Hits an "allow" comment covers, for enabled rules. Reported as a count per rule so suppression is a metric
    /// rather than an invisible escape hatch: an agent that answers every hit with an allow comment shows up here.
    /// </summary>
    public List<Finding> Suppressed { get; } = new List<Finding>();

    public InfoStats Info { get; } = new InfoStats();
    public bool HasGatingFindings => Findings.Any(f => f.Severity >= Severity.Warning);

    public int SuppressedCount(RuleDescriptor rule) => Suppressed.Count(f => f.Rule == rule);
}

public sealed class LintOptions
{
    public Profile Profile { get; set; } = Profile.Recommended;
    public bool IncludeDev { get; set; }

    /// <summary>Explicit severity overrides keyed by rule id (UNOL001) or name (HEX); win over .editorconfig.</summary>
    public Dictionary<string, Severity> SeverityOverrides { get; } = new Dictionary<string, Severity>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Per-file severities read from .editorconfig, honoring section globs like a Roslyn analyzer does.</summary>
    public EditorConfigSet? EditorConfig { get; set; }

    /// <summary>Force the Toolkit gate open or closed instead of detecting it from the project.</summary>
    public bool? AssumeToolkit { get; set; }
}
