using System.Text;
using System.Text.RegularExpressions;

namespace Uno.Lint;

/// <summary>
/// The .editorconfig settings that apply to a lint run, with section globs honored per file the way a Roslyn
/// analyzer honors them, so the CLI, the hook and a future analyzer agree on every count.
///   [*.xaml]
///   dotnet_diagnostic.UNOL001.severity = none | silent | suggestion | warning | error
///   uno_lint.profile = recommended | strict
/// The profile is project-wide: the last one read wins regardless of its section.
/// </summary>
public sealed class EditorConfigSet
{
    private readonly List<Section> _sections = new List<Section>();

    public Profile? Profile { get; private set; }

    /// <summary>Severity for a rule id on a file. Sections are applied in file order, nearest .editorconfig last; the last match wins.</summary>
    public bool TryGetSeverity(string ruleId, string? filePath, out Severity severity)
    {
        severity = Severity.None;
        var found = false;
        var normalizedPath = filePath == null ? null : Path.GetFullPath(filePath).Replace('\\', '/');

        foreach (var section in _sections)
        {
            if (!section.Severities.TryGetValue(ruleId, out var value))
            {
                continue;
            }

            if (section.Matches(normalizedPath))
            {
                severity = value;
                found = true;
            }
        }

        return found;
    }

    /// <summary>Rule ids that have a severity only inside a glob section, which a global-only reader would misapply.</summary>
    public IEnumerable<string> SectionedRuleIds() =>
        _sections.Where(s => s.Glob != null).SelectMany(s => s.Severities.Keys).Distinct(StringComparer.OrdinalIgnoreCase);

    public static EditorConfigSet Load(string rootOrFile)
    {
        var set = new EditorConfigSet();
        foreach (var path in FindFiles(rootOrFile))
        {
            set.AddFile(path);
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
        catch (IOException)
        {
            return;
        }

        AddText(lines, Path.GetDirectoryName(Path.GetFullPath(path)) ?? string.Empty);
    }

    /// <summary>Parses .editorconfig text whose section globs are relative to <paramref name="directory"/>.</summary>
    public void AddText(IEnumerable<string> lines, string directory)
    {
        var current = new Section(directory, null);
        _sections.Add(current);

        foreach (var raw in lines)
        {
            var line = StripComment(raw).Trim();
            if (line.Length == 0)
            {
                continue;
            }

            if (line[0] == '[' && line[line.Length - 1] == ']')
            {
                current = new Section(directory, line.Substring(1, line.Length - 2).Trim());
                _sections.Add(current);
                continue;
            }

            var d = DiagnosticLine.Match(line);
            if (d.Success && TryParseSeverity(d.Groups[2].Value, out var severity))
            {
                current.Severities[d.Groups[1].Value.ToUpperInvariant()] = severity;
                continue;
            }

            var p = ProfileLine.Match(line);
            if (p.Success && Enum.TryParse<Profile>(p.Groups[1].Value, ignoreCase: true, out var profile))
            {
                Profile = profile;
            }
        }
    }

    private static readonly Regex DiagnosticLine = new Regex(@"^dotnet_diagnostic\.(UNOL\d{3})\.severity\s*=\s*(\w+)", RegexOptions.IgnoreCase);
    private static readonly Regex ProfileLine = new Regex(@"^uno_lint\.profile\s*=\s*(\w+)", RegexOptions.IgnoreCase);

    private static string StripComment(string line)
    {
        var cut = line.IndexOfAny(new[] { '#', ';' });
        return cut >= 0 ? line.Substring(0, cut) : line;
    }

    /// <summary>The .editorconfig files from the root upward, farthest first so the nearest file's sections are applied last.</summary>
    private static IEnumerable<string> FindFiles(string rootOrFile)
    {
        var found = new List<string>();
        var dir = Directory.Exists(rootOrFile) ? Path.GetFullPath(rootOrFile) : Path.GetDirectoryName(Path.GetFullPath(rootOrFile));
        while (!string.IsNullOrEmpty(dir))
        {
            var candidate = Path.Combine(dir!, ".editorconfig");
            if (File.Exists(candidate))
            {
                found.Add(candidate);
                if (Regex.IsMatch(File.ReadAllText(candidate), @"^\s*root\s*=\s*true", RegexOptions.IgnoreCase | RegexOptions.Multiline))
                {
                    break;
                }
            }

            dir = Path.GetDirectoryName(dir);
        }

        found.Reverse();
        return found;
    }

    private static bool TryParseSeverity(string text, out Severity severity)
    {
        switch (text.ToLowerInvariant())
        {
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
                severity = Severity.None;
                return false;
        }
    }

    private sealed class Section
    {
        private readonly string _directory;
        private readonly Regex? _pattern;

        public Section(string directory, string? glob)
        {
            _directory = directory.Replace('\\', '/').TrimEnd('/');
            Glob = glob;
            _pattern = glob == null ? null : GlobToRegex(glob);
        }

        public string? Glob { get; }

        public Dictionary<string, Severity> Severities { get; } = new Dictionary<string, Severity>(StringComparer.OrdinalIgnoreCase);

        public bool Matches(string? normalizedFilePath)
        {
            if (_pattern == null)
            {
                return true; // preamble: applies to every file
            }

            if (normalizedFilePath == null)
            {
                return false; // a sectioned severity needs a file to match against
            }

            if (!normalizedFilePath.StartsWith(_directory + "/", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var relative = normalizedFilePath.Substring(_directory.Length + 1);
            return _pattern.IsMatch(relative);
        }

        /// <summary>
        /// EditorConfig glob semantics: a pattern without '/' matches the file name in any folder; one with '/' is
        /// relative to the .editorconfig folder. '*' stays within a segment, '**' crosses segments, '{a,b}' alternates.
        /// </summary>
        private static Regex GlobToRegex(string glob)
        {
            var pattern = glob.Replace('\\', '/');
            if (pattern.StartsWith("/", StringComparison.Ordinal))
            {
                pattern = pattern.Substring(1);
            }
            else if (pattern.IndexOf('/') < 0)
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

                    var alternatives = pattern.Substring(i + 1, close - i - 1).Split(',').Select(a => Regex.Escape(a.Trim()).Replace(@"\*", "[^/]*"));
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

                    var cls = pattern.Substring(i, close - i + 1);
                    sb.Append(cls.StartsWith("[!", StringComparison.Ordinal) ? "[^" + cls.Substring(2) : cls);
                    i = close + 1;
                }
                else
                {
                    sb.Append(Regex.Escape(c.ToString()));
                    i++;
                }
            }

            sb.Append('$');
            return new Regex(sb.ToString(), RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }
    }
}

/// <summary>Convenience entry points over <see cref="EditorConfigSet"/>.</summary>
public static class EditorConfig
{
    /// <summary>Loads the .editorconfig chain for <paramref name="root"/> into the options and applies its profile unless one was given explicitly.</summary>
    public static void Apply(string root, LintOptions options, bool profileExplicit)
    {
        var set = EditorConfigSet.Load(root);
        options.EditorConfig = set;
        if (!profileExplicit && set.Profile.HasValue)
        {
            options.Profile = set.Profile.Value;
        }
    }
}
