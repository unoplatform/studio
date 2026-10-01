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
    /// <summary>Rules almost every Uno team accepts: colors, theme-awareness, icons, shadowed controls, breakpoints.</summary>
    Recommended,
    /// <summary>Recommended plus the MVUX + Extensions.Navigation + Toolkit architectural rules.</summary>
    Strict,
}

public sealed class RuleDescriptor
{
    public RuleDescriptor(string id, string name, string title, string fix, Profile minimumProfile, bool requiresToolkit = false)
    {
        Id = id;
        Name = name;
        Title = title;
        Fix = fix;
        MinimumProfile = minimumProfile;
        RequiresToolkit = requiresToolkit;
    }

    /// <summary>Diagnostic id, e.g. UNOL001. Used by .editorconfig overrides.</summary>
    public string Id { get; }

    /// <summary>Short upper-case name used in reports and in suppression comments (lower-cased).</summary>
    public string Name { get; }

    public string Title { get; }

    public string Fix { get; }

    public Profile MinimumProfile { get; }

    /// <summary>The rule's suggested replacement is a Uno.Toolkit control; skipped when the project has no Toolkit reference.</summary>
    public bool RequiresToolkit { get; }

    public string SuppressionKeyword => Name.ToLowerInvariant();
}

public sealed class Finding
{
    public Finding(RuleDescriptor rule, string file, int line, string message)
    {
        Rule = rule;
        File = file;
        Line = line;
        Message = message;
    }

    public RuleDescriptor Rule { get; }

    /// <summary>Path relative to the lint root (or the file name in single-file mode).</summary>
    public string File { get; }

    public int Line { get; }

    public string Message { get; }

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
    public List<Finding> Findings { get; } = new List<Finding>();
    public InfoStats Info { get; } = new InfoStats();
    public bool HasGatingFindings => Findings.Any(f => f.Severity >= Severity.Warning);
}

public sealed class LintOptions
{
    public Profile Profile { get; set; } = Profile.Recommended;
    public bool IncludeDev { get; set; }

    /// <summary>Severity overrides keyed by rule id (UNOL001) or name (HEX). Usually read from .editorconfig.</summary>
    public Dictionary<string, Severity> SeverityOverrides { get; } = new Dictionary<string, Severity>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Force the Toolkit gate open or closed instead of detecting it from the project.</summary>
    public bool? AssumeToolkit { get; set; }
}
