using System.Text.RegularExpressions;

namespace Uno.Lint;

/// <summary>
/// Minimal .editorconfig reader for the two keys we honor, using the same syntax as Roslyn analyzers:
///   dotnet_diagnostic.UNOL103.severity = none | suggestion | warning | error
///   uno_lint.profile = recommended | strict
/// Section headers are ignored: the settings are treated as project-wide.
/// </summary>
public static class EditorConfig
{
    private static readonly Regex DiagnosticLine = new Regex(@"^\s*dotnet_diagnostic\.(UNOL\d{3})\.severity\s*=\s*(\w+)", RegexOptions.IgnoreCase);
    private static readonly Regex ProfileLine = new Regex(@"^\s*uno_lint\.profile\s*=\s*(\w+)", RegexOptions.IgnoreCase);

    public static void Apply(string root, LintOptions options, bool profileExplicit)
    {
        foreach (var path in FindFiles(root))
        {
            string[] lines;
            try
            {
                lines = File.ReadAllLines(path);
            }
            catch (IOException)
            {
                continue;
            }

            foreach (var raw in lines)
            {
                var line = raw;
                var hash = line.IndexOf('#');
                if (hash >= 0)
                {
                    line = line.Substring(0, hash);
                }

                var d = DiagnosticLine.Match(line);
                if (d.Success && TryParseSeverity(d.Groups[2].Value, out var severity))
                {
                    options.SeverityOverrides[d.Groups[1].Value.ToUpperInvariant()] = severity;
                    continue;
                }

                var p = ProfileLine.Match(line);
                if (p.Success && !profileExplicit && Enum.TryParse<Profile>(p.Groups[1].Value, ignoreCase: true, out var profile))
                {
                    options.Profile = profile;
                }
            }
        }
    }

    /// <summary>The .editorconfig files from the root upward, nearest last so it wins.</summary>
    private static IEnumerable<string> FindFiles(string root)
    {
        var found = new List<string>();
        var dir = Directory.Exists(root) ? root : Path.GetDirectoryName(root);
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
}
