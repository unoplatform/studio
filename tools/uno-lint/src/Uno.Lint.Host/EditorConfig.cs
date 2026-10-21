using System.Text;
using System.Text.RegularExpressions;

namespace Uno.Lint;

/// <summary>
/// The .editorconfig settings that apply to a lint run, with section globs honored per file the way a Roslyn
/// analyzer honors them, so the CLI, the hook and a future analyzer agree on every count.
///   [*.xaml]
///   dotnet_diagnostic.UNOL001.severity = default | none | silent | suggestion | warning | error
///   uno_lint.profile = recommended | strict
/// Severity keys count only inside a [section]; EditorConfig allows only "root" in the preamble and the compiler
/// does not read severities there, so a preamble severity is recorded in <see cref="IgnoredPreambleKeys"/> and
/// otherwise ignored. Every file is governed by its own chain: the .editorconfig files from its folder upward,
/// stopping at <c>root = true</c>, read once per folder whether the run starts at the file or at a folder above it.
/// The profile follows the same chain, wherever it sits in a file: the nearest one wins.
/// </summary>
public sealed partial class EditorConfigSet
{
    private readonly Dictionary<string, IReadOnlyList<ConfigFile>> _chains = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<ConfigFile> _loaded = new();
    private readonly List<ConfigFile> _added = new();
    private readonly string? _start;

    public EditorConfigSet()
    {
    }

    private EditorConfigSet(string start)
    {
        _start = start;
    }

    /// <summary>The profile where the run starts: the folder linted, or the file's folder.</summary>
    public Profile? Profile => _start == null ? LastProfile(_added) : ProfileFor(_start);

    /// <summary>Severity keys found above any section header in the files read so far, which a Roslyn analyzer would not honor.</summary>
    public IReadOnlyList<string> IgnoredPreambleKeys => _loaded.Concat(_added).SelectMany(f => f.PreambleKeys).ToList();

    /// <summary>The profile governing a file, or a folder's own files: the nearest uno_lint.profile in its chain.</summary>
    public Profile? ProfileFor(string path)
    {
        var full = Path.GetFullPath(path);
        return LastProfile(ChainFor(Directory.Exists(full) ? full : Path.GetDirectoryName(full)).Concat(_added));
    }

    /// <summary>
    /// Severity configured for a rule id on a file. The file's chain is applied farthest first, nearest last; the last
    /// matching section wins. <paramref name="severity"/> is null when the winning value is <c>default</c>: the rule
    /// keeps its default severity for the active profile.
    /// </summary>
    public bool TryGetSeverity(string ruleId, string filePath, out Severity? severity)
    {
        severity = null;
        var found = false;
        var full = Path.GetFullPath(filePath);
        var normalizedPath = full.Replace('\\', '/');

        foreach (var file in ChainFor(Path.GetDirectoryName(full)).Concat(_added))
        {
            foreach (var section in file.Sections)
            {
                if (section.Severities.TryGetValue(ruleId, out var value) && section.Matches(normalizedPath))
                {
                    severity = value;
                    found = true;
                }
            }
        }

        return found;
    }

    /// <summary>Rule ids that have a severity inside a glob section, in the files read so far.</summary>
    public IEnumerable<string> SectionedRuleIds() =>
        _loaded.Concat(_added).SelectMany(f => f.Sections).SelectMany(s => s.Severities.Keys).Distinct(StringComparer.OrdinalIgnoreCase);

    /// <summary>The settings for a run starting at a folder or file. Each folder's chain is read on first use, once.</summary>
    public static EditorConfigSet Load(string rootOrFile) => new EditorConfigSet(Path.GetFullPath(rootOrFile));

    /// <summary>Parses .editorconfig text whose section globs are relative to <paramref name="directory"/>; it applies to every file, after its chain.</summary>
    public void AddText(IEnumerable<string> lines, string directory) => _added.Add(Parse(lines, directory));

    private static Profile? LastProfile(IEnumerable<ConfigFile> files) => files.LastOrDefault(f => f.Profile != null)?.Profile;

    /// <summary>A folder's chain, farthest first: its parent's chain plus its own file, or its own file alone when that is a root.</summary>
    private IReadOnlyList<ConfigFile> ChainFor(string? directory)
    {
        if (directory is not { Length: > 0 })
        {
            return [];
        }

        if (_chains.TryGetValue(directory, out var chain))
        {
            return chain;
        }

        var own = Read(Path.Combine(directory, ".editorconfig"));
        if (own is { IsRoot: true })
        {
            chain = [own];
        }
        else
        {
            var parent = ChainFor(Path.GetDirectoryName(directory));
            chain = own == null ? parent : [.. parent, own];
        }

        _chains[directory] = chain;
        return chain;
    }

    private ConfigFile? Read(string path)
    {
        string[] lines;
        try
        {
            if (!File.Exists(path))
            {
                return null;
            }

            lines = File.ReadAllLines(path);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return null;
        }

        var file = Parse(lines, Path.GetDirectoryName(path) ?? string.Empty);
        _loaded.Add(file);
        return file;
    }

    private static ConfigFile Parse(IEnumerable<string> lines, string directory)
    {
        var file = new ConfigFile();
        Section? current = null;

        foreach (var raw in lines)
        {
            var line = StripComment(raw).Trim();
            if (line.Length == 0)
            {
                continue;
            }

            if (line[0] == '[' && line[^1] == ']')
            {
                current = new Section(directory, line[1..^1].Trim());
                file.Sections.Add(current);
                continue;
            }

            if (current == null && RootLine().IsMatch(line))
            {
                file.IsRoot = true;
                continue;
            }

            var diagnostic = DiagnosticLine().Match(line);
            if (diagnostic.Success && TryParseSeverity(diagnostic.Groups[2].Value, out var severity))
            {
                var id = diagnostic.Groups[1].Value.ToUpperInvariant();
                if (current == null)
                {
                    file.PreambleKeys.Add(id);
                }
                else
                {
                    current.Severities[id] = severity;
                }

                continue;
            }

            var profile = ProfileLine().Match(line);
            if (profile.Success && Enum.TryParse<Profile>(profile.Groups[1].Value, ignoreCase: true, out var value))
            {
                file.Profile = value;
            }
        }

        return file;
    }

    [GeneratedRegex(@"^dotnet_diagnostic\.(UNOL\d{3})\.severity\s*=\s*(\w+)", RegexOptions.IgnoreCase)]
    private static partial Regex DiagnosticLine();

    [GeneratedRegex(@"^uno_lint\.profile\s*=\s*(\w+)", RegexOptions.IgnoreCase)]
    private static partial Regex ProfileLine();

    [GeneratedRegex(@"^root\s*=\s*true$", RegexOptions.IgnoreCase)]
    private static partial Regex RootLine();

    private static string StripComment(string line)
    {
        var cut = line.IndexOfAny(['#', ';']);
        return cut >= 0 ? line[..cut] : line;
    }

    /// <summary><c>default</c> parses to null: the rule's default severity for the active profile, as for the compiler.</summary>
    private static bool TryParseSeverity(string text, out Severity? severity)
    {
        switch (text.ToLowerInvariant())
        {
            case "default":
                severity = null;
                return true;
            case "none":
            case "silent":
                severity = Severity.None;
                return true;
            case "suggestion":
                severity = Severity.Suggestion;
                return true;
            case "warning":
                severity = Severity.Warning;
                return true;
            case "error":
                severity = Severity.Error;
                return true;
            default:
                severity = null;
                return false;
        }
    }

    /// <summary>One parsed .editorconfig file.</summary>
    private sealed class ConfigFile
    {
        public List<Section> Sections { get; } = new();

        public List<string> PreambleKeys { get; } = new();

        public bool IsRoot { get; set; }

        public Profile? Profile { get; set; }
    }

    private sealed class Section
    {
        private readonly string _directory;
        private readonly Regex _pattern;

        public Section(string directory, string glob)
        {
            _directory = directory.Replace('\\', '/').TrimEnd('/');
            _pattern = GlobToRegex(glob);
        }

        public Dictionary<string, Severity?> Severities { get; } = new(StringComparer.OrdinalIgnoreCase);

        public bool Matches(string normalizedFilePath)
        {
            if (!normalizedFilePath.StartsWith(_directory + "/", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return _pattern.IsMatch(normalizedFilePath[(_directory.Length + 1)..]);
        }

        /// <summary>
        /// EditorConfig glob semantics: a pattern without '/' matches the file name in any folder; one with '/' is
        /// relative to the .editorconfig folder. '*' stays within a segment, '**' crosses segments, '{a,b}' alternates.
        /// Interpreted rather than compiled: each section's pattern is used a handful of times per run.
        /// </summary>
        private static Regex GlobToRegex(string glob)
        {
            var pattern = glob.Replace('\\', '/');
            if (pattern.StartsWith('/'))
            {
                pattern = pattern[1..];
            }
            else if (!pattern.Contains('/'))
            {
                pattern = "**/" + pattern;
            }

            var sb = new StringBuilder("^");
            var i = 0;
            while (i < pattern.Length)
            {
                var c = pattern[i];
                if (c == '*')
                {
                    if (i + 1 < pattern.Length && pattern[i + 1] == '*')
                    {
                        // "**/" may match nothing at all (file at the root).
                        if (i + 2 < pattern.Length && pattern[i + 2] == '/')
                        {
                            sb.Append("(?:.*/)?");
                            i += 3;
                        }
                        else
                        {
                            sb.Append(".*");
                            i += 2;
                        }
                    }
                    else
                    {
                        sb.Append("[^/]*");
                        i++;
                    }
                }
                else if (c == '?')
                {
                    sb.Append("[^/]");
                    i++;
                }
                else if (c == '{')
                {
                    var close = pattern.IndexOf('}', i);
                    if (close < 0)
                    {
                        sb.Append(Regex.Escape("{"));
                        i++;
                        continue;
                    }

                    var alternatives = pattern[(i + 1)..close].Split(',').Select(a => Regex.Escape(a.Trim()).Replace(@"\*", "[^/]*"));
                    sb.Append("(?:").Append(string.Join("|", alternatives)).Append(')');
                    i = close + 1;
                }
                else if (c == '[')
                {
                    var close = pattern.IndexOf(']', i);
                    if (close < 0)
                    {
                        sb.Append(Regex.Escape("["));
                        i++;
                        continue;
                    }

                    var cls = pattern[i..(close + 1)];
                    sb.Append(cls.StartsWith("[!", StringComparison.Ordinal) ? "[^" + cls[2..] : cls);
                    i = close + 1;
                }
                else
                {
                    sb.Append(Regex.Escape(c.ToString()));
                    i++;
                }
            }

            sb.Append('$');
            return new Regex(sb.ToString(), RegexOptions.IgnoreCase);
        }
    }
}
