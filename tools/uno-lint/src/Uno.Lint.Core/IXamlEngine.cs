namespace Uno.Lint;

/// <summary>
/// The XAML host. uno-lint's XAML rules run inside Uno.XamlParser (net9.0+), which this netstandard2.0 core cannot
/// reference, so the host is plugged in through this interface (see <see cref="XamlEngines"/>). The core keeps
/// everything Uno.XamlParser does not do: C# rules, configuration, suppressions, project context and output.
/// </summary>
public interface IXamlEngine
{
    string Name { get; }

    /// <param name="relativePath">Path used in findings and for file-name roles (palette, icons).</param>
    /// <param name="text">The file as saved; findings carry line numbers into this text.</param>
    /// <param name="isEnabled">Whether a rule is enabled for this file after profile, .editorconfig and project gates.</param>
    XamlEngineResult Analyze(string relativePath, string text, ProjectContext context, Func<RuleDescriptor, bool> isEnabled);
}

/// <summary>Where the CLI (and tests) register the XAML host, so <see cref="Linter"/> needs no reference to it.</summary>
public static class XamlEngines
{
    private static readonly object Gate = new object();
    private static Func<IXamlEngine>? _factory;
    private static IXamlEngine? _instance;

    /// <summary>Creates the default engine on first use; the engine is reused across runs.</summary>
    public static Func<IXamlEngine>? DefaultFactory
    {
        get => _factory;
        set
        {
            lock (Gate)
            {
                _factory = value;
                _instance = null;
            }
        }
    }

    public static IXamlEngine? Default
    {
        get
        {
            lock (Gate)
            {
                return _instance ??= _factory?.Invoke();
            }
        }
    }
}

public sealed class XamlEngineResult
{
    public XamlEngineResult(bool parsed, bool repaired, bool linesShifted, bool partial, IReadOnlyList<Finding> findings, IReadOnlyList<string> repairCodes, XamlInfo info)
    {
        Parsed = parsed;
        Repaired = repaired;
        LinesShifted = linesShifted;
        Partial = partial;
        Findings = findings;
        RepairCodes = repairCodes;
        Info = info;
    }

    /// <summary>The engine produced a document model, possibly after repairing the markup.</summary>
    public bool Parsed { get; }

    /// <summary>The markup was malformed and the engine repaired it before the rules ran.</summary>
    public bool Repaired { get; }

    /// <summary>The repair changed the line count, so reported lines may not match the saved file.</summary>
    public bool LinesShifted { get; }

    /// <summary>Even after repair the reader stopped early; the rules ran on the elements read before the stop.</summary>
    public bool Partial { get; }

    /// <summary>Every finding of the enabled rules, suppressed ones flagged as such.</summary>
    public IReadOnlyList<Finding> Findings { get; }

    /// <summary>Diagnostic codes of the repairs applied (e.g. XML1006 mismatched tags).</summary>
    public IReadOnlyList<string> RepairCodes { get; }

    public XamlInfo Info { get; }
}

/// <summary>The non-gating XAML counters for one file.</summary>
public sealed class XamlInfo
{
    public static readonly XamlInfo Empty = new XamlInfo(0, 0, 0, 0, Array.Empty<string>(), Array.Empty<string>());

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
