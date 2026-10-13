using System.Text.RegularExpressions;

namespace Uno.Lint;

/// <summary>
/// A finding is suppressed by a comment on the same line or in the comment lines directly above it (up to three),
/// reason required:
///   &lt;!-- uno-lint: allow card - image clip with a placeholder fill --&gt;
///   // uno-lint: allow codebehind - video transport glue, no Toolkit equivalent
/// The look-behind stops at the first line that is not a comment, so a comment never reaches past another element
/// or statement. For a finding on an attribute of a multi-line element, the element's start line is an anchor too:
/// XML has no place for a comment inside a start tag. "allow all - reason" covers every rule on that line.
/// The legacy "xaml-lint:" prefix from the PowerShell prototype is accepted too.
/// </summary>
public static class Suppression
{
    private const int LookBehindLines = 3;

    // A line that is only comment: XML comment open/close/body, or a C# line comment or block-comment line.
    private static readonly Regex CommentOnlyLine = new Regex(@"^\s*(?:<!--.*?(?:-->\s*)?|.*?-->|//.*|/\*.*|\*.*)$", RegexOptions.Compiled);

    public static bool IsSuppressed(string[] lines, int oneBasedLine, RuleDescriptor rule) =>
        IsSuppressed(lines, rule, oneBasedLine);

    /// <summary>True when any anchor line, or the comment lines directly above any anchor, carries an allow comment for the rule.</summary>
    public static bool IsSuppressed(string[] lines, RuleDescriptor rule, params int[] anchorLines)
    {
        var keyword = Regex.Escape(rule.SuppressionKeyword);
        // The reason must start with something other than '-' or '>', otherwise a bare "allow hex -->" would count.
        var pattern = new Regex(@"(?:uno|xaml)-lint:\s*allow\s+(?:" + keyword + @"|all)\s*-\s*[^\s\->]", RegexOptions.IgnoreCase);

        foreach (var anchor in anchorLines.Distinct())
        {
            if (anchor >= 1 && anchor - 1 < lines.Length && pattern.IsMatch(lines[anchor - 1]))
            {
                return true;
            }

            var seen = 0;
            for (var i = anchor - 2; i >= 0 && seen < LookBehindLines; i--)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                if (!CommentOnlyLine.IsMatch(line))
                {
                    break; // another element or statement: the comment above it is not ours
                }

                seen++;
                if (pattern.IsMatch(line))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public static string[] SplitLines(string text) => text.Replace("\r\n", "\n").Split('\n');
}
