namespace Uno.Lint;

/// <summary>
/// A pluggable XAML host. It owns a subset of the XAML rules and runs them on its own document model; the built-in
/// XDocument path runs every other rule. This is how a host that is not netstandard2.0 (Uno.XamlParser is net9.0)
/// plugs into the rule library without the core taking a dependency on it.
/// </summary>
public interface IXamlEngine
{
    string Name { get; }

    /// <summary>Rules this engine evaluates. The built-in path skips them while the engine is active.</summary>
    IReadOnlyCollection<RuleDescriptor> OwnedRules { get; }

    /// <param name="relativePath">Path used in findings and for file-name roles (palette, icons).</param>
    /// <param name="text">The file as saved; findings must carry line numbers into this text.</param>
    /// <param name="isEnabled">Whether a rule is enabled for this file after profile, .editorconfig and project gates.</param>
    XamlEngineResult Analyze(string relativePath, string text, ProjectContext context, Func<RuleDescriptor, bool> isEnabled);
}

public sealed class XamlEngineResult
{
    public XamlEngineResult(bool parsed, bool repaired, bool linesShifted, IReadOnlyList<Finding> findings, IReadOnlyList<string> repairCodes, bool partial = false)
    {
        Parsed = parsed;
        Repaired = repaired;
        LinesShifted = linesShifted;
        Findings = findings;
        RepairCodes = repairCodes;
        Partial = partial;
    }

    /// <summary>The engine produced a document model, possibly after repairing the markup.</summary>
    public bool Parsed { get; }

    /// <summary>Even after repair the reader stopped early; the rules ran on the elements read before the stop.</summary>
    public bool Partial { get; }

    /// <summary>The markup was malformed and the engine repaired it before analysis.</summary>
    public bool Repaired { get; }

    /// <summary>The repair changed the line count, so reported lines may not match the saved file.</summary>
    public bool LinesShifted { get; }

    public IReadOnlyList<Finding> Findings { get; }

    /// <summary>Diagnostic codes of the repairs applied (e.g. XML1006 mismatched tags).</summary>
    public IReadOnlyList<string> RepairCodes { get; }
}
