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

/// <summary>What a finding means for the person or agent reading it.</summary>
public enum RuleCategory
{
    /// <summary>A pattern the platform already covers: the code works, but a platform control, resource or binding should replace it.</summary>
    Style,
    /// <summary>A bug: the code does not do what its author meant.</summary>
    Correctness,
}

public sealed class RuleDescriptor
{
    public RuleDescriptor(string id, string name, string title, string fix, Profile minimumProfile, bool requiresToolkit = false, bool requiresMvux = false, Severity defaultSeverity = Severity.Warning, RuleCategory category = RuleCategory.Style)
    {
        Id = id;
        Name = name;
        Title = title;
        Fix = fix;
        MinimumProfile = minimumProfile;
        RequiresToolkit = requiresToolkit;
        RequiresMvux = requiresMvux;
        DefaultSeverity = defaultSeverity;
        Category = category;
        SuppressionKeyword = name.ToLowerInvariant();
    }

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

    /// <summary>Severity when the rule is enabled and nothing overrides it. Warning gates; Suggestion is reported but never fails a run.</summary>
    public Severity DefaultSeverity { get; }

    public RuleCategory Category { get; }

    public string SuppressionKeyword { get; }
}

/// <summary>One hit of one rule. Immutable: hosts derive a new instance with <see cref="WithResolution"/>.</summary>
public sealed class Finding
{
    public Finding(RuleDescriptor rule, string file, int line, string message, bool suppressed = false)
        : this(rule, file, line, message, suppressed, rule.Fix, rule.DefaultSeverity)
    {
    }

    private Finding(RuleDescriptor rule, string file, int line, string message, bool suppressed, string fix, Severity severity)
    {
        Rule = rule;
        File = file;
        Line = line;
        Message = message;
        Suppressed = suppressed;
        Fix = fix;
        Severity = severity;
    }

    public RuleDescriptor Rule { get; }

    /// <summary>Path relative to the lint root (or the file name in single-file mode).</summary>
    public string File { get; }

    public int Line { get; }

    public string Message { get; }

    /// <summary>True when an "allow" comment covers this hit. Suppressed findings are counted, not reported as gating.</summary>
    public bool Suppressed { get; }

    /// <summary>Fix text adapted to the project; the rule's own text until a host resolves it.</summary>
    public string Fix { get; }

    /// <summary>Effective severity; the rule's default until a host resolves it for the file.</summary>
    public Severity Severity { get; }

    /// <summary>This finding with the severity and fix a host resolved for its file and project.</summary>
    public Finding WithResolution(Severity severity, string fix) => new Finding(Rule, File, Line, Message, Suppressed, fix, severity);

    public override string ToString() => $"{File}({Line}): {Rule.Id} {Rule.Name}: {Message}";
}
