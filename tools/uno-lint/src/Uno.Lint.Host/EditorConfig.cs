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
/// otherwise ignored. The profile is project-wide: the last one read wins wherever it sits.
/// For a folder, files are loaded from the folder upward (nearest last) and downward, so a nested
/// <c>App/.editorconfig</c> applies whether the run starts at the repo root or at the file. For a file, only its
/// ancestors are read: a sibling folder's .editorconfig can never match it.
/// </summary>
public sealed partial class EditorConfigSet
{
    private readonly List<Section> _sections = new();
    private readonly List<string> _ignoredPreambleKeys = new();

    public Profile? Profile { get; private set; }

    /// <summary>Severity keys found above any section header, which a Roslyn analyzer would not honor.</summary>
    public IReadOnlyList<string> IgnoredPreambleKeys => _ignoredPreambleKeys;

    /// <summary>
    /// Severity configured for a rule id on a file. Sections are applied in load order, nearest .editorconfig last; the
    /// last match wins. <paramref name="severity"/> is null when the winning value is <c>default</c>: the rule keeps
    /// its default severity for the active profile.
    /// </summary>
    public bool TryGetSeverity(string ruleId, string filePath, out Severity? severity)
    {
        severity = null;
        var found = false;
        var normalizedPath = Path.GetFullPath(filePath).Replace('\\', '/');

        foreach (var section in _sections)
        {
            if (section.Severities.TryGetValue(ruleId, out var value) && section.Matches(normalizedPath))
            {
                severity = value;
                found = true;
            }
        }

        return found;
    }

    /// <summary>Rule ids that have a severity inside a glob section.</summary>
    public IEnumerable<string> SectionedRuleIds() =>
        _sections.SelectMany(s => s.Severities.Keys).Distinct(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Loads the chain for a folder or file: ancestors first (farthest to nearest, stopping at <c>root = true</c>),
    /// then, for a folder, every .editorconfig below it in path order so deeper files are applied last.
    /// </summary>
    public static EditorConfigSet Load(string rootOrFile)
    {
        var set = new EditorConfigSet();
        var full = Path.GetFullPath(rootOrFile);
        foreach (var path in FindAncestors(full))
        {
            set.AddFile(path);
        }

        if (Directory.Exists(full))
        {
            var nested = FileWalker.Enumerate(full, f => string.Equals(Path.GetFileName(f), ".editorconfig", StringComparison.OrdinalIgnoreCase))
                .Where(f => !string.Equals(Path.GetDirectoryName(f), full, StringComparison.OrdinalIgnoreCase))
                .OrderBy(f => f.Count(c => c == '\\' || c == '/'))
                .ThenBy(f => f, StringComparer.OrdinalIgnoreCase);
            foreach (var path in nested)
            {
                set.AddFile(path);
            }
        }

        return set;
    }

    public void AddFile(string path)
    {
        string[] lines;
        try
        {
            lines = File.ReadAllLines(path);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return;
        }

        AddText(lines, Path.GetDirectoryName(Path.GetFullPath(path)) ?? string.Empty);
    }

    /// <summary>Parses .editorconfig text whose section globs are relative to <paramref name="directory"/>.</summary>
    public void AddText(IEnumerable<string> lines, string directory)
    {
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
                _sections.Add(current);
                continue;
            }

            var diagnostic = DiagnosticLine().Match(line);
            if (diagnostic.Success && TryParseSeverity(diagnostic.Groups[2].Value, out var severity))
            {
                var id = diagnostic.Groups[1].Value.ToUpperInvariant();
                if (current == null)
                {
                    _ignoredPreambleKeys.Add(id);
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
                Profile = value;
            }
        }
    }

    [GeneratedRegex(@"^dotnet_diagnostic\.(UNOL\d{3})\.severity\s*=\s*(\w+)", RegexOptions.IgnoreCase)]
    private static partial Regex DiagnosticLine();

    [GeneratedRegex(@"^uno_lint\.profile\s*=\s*(\w+)", RegexOptions.IgnoreCase)]
    private static partial Regex ProfileLine();

    [GeneratedRegex(@"^\s*root\s*=\s*true", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
    private static partial Regex RootLine();

    private static string StripComment(string line)
    {
        var cut = line.IndexOfAny(['#', ';']);
        return cut >= 0 ? line[..cut] : line;
    }

    /// <summary>The .editorconfig files from the root upward, farthest first so the nearest file's sections are applied last.</summary>
    private static List<string> FindAncestors(string rootOrFile)
    {
        var found = new List<string>();
        var dir = Directory.Exists(rootOrFile) ? rootOrFile : Path.GetDirectoryName(rootOrFile);
        while (dir is { Length: > 0 })
        {
            var candidate = Path.Combine(dir, ".editorconfig");
            if (File.Exists(candidate))
            {
                found.Add(candidate);
                if (IsRoot(candidate))
                {
                    break;
                }
            }

            dir = Path.GetDirectoryName(dir);
        }

        found.Reverse();
        return found;
    }

    private static bool IsRoot(string path)
    {
        try
        {
            return RootLine().IsMatch(File.ReadAllText(path));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return false;
        }
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
