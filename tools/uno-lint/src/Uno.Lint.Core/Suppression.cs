using System.Text.RegularExpressions;

namespace Uno.Lint;

/// <summary>
/// A finding is suppressed by a comment within the three non-blank lines above it (or on the same line), reason required:
///   &lt;!-- uno-lint: allow card - image clip with a placeholder fill --&gt;
///   // uno-lint: allow codebehind - video transport glue, no Toolkit equivalent
/// The legacy "xaml-lint:" prefix from the PowerShell prototype is accepted too.
/// </summary>
public static class Suppression
{
    private const int LookBehindLines = 3;

    public static bool IsSuppressed(string[] lines, int oneBasedLine, RuleDescriptor rule)
    {
        var keyword = Regex.Escape(rule.SuppressionKeyword);
        // The reason must start with something other than '-' or '>', otherwise a bare "allow hex -->" would count.
        var pattern = new Regex(@"(?:uno|xaml)-lint:\s*allow\s+(?:" + keyword + @"|all)\s*-\s*[^\s\->]", RegexOptions.IgnoreCase);

        if (oneBasedLine - 1 < lines.Length && oneBasedLine >= 1 && pattern.IsMatch(lines[oneBasedLine - 1]))
        {
            return true;
        }

        var seen = 0;
        for (var i = oneBasedLine - 2; i >= 0 && seen < LookBehindLines; i--)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            seen++;
            if (pattern.IsMatch(line))
            {
                return true;
            }
        }

        return false;
    }

    public static string[] SplitLines(string text) => text.Replace("\r\n", "\n").Split('\n');
}
