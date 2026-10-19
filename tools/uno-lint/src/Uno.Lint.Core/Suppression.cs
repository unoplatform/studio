using System.Collections.Concurrent;
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
    private static readonly Regex CommentOnlyLine = new Regex(@"^\s*(?:<!--.*?(?:-->\s*)?|.*?-->|//.*|/\*.*|\*.*)$");

    private static readonly ConcurrentDictionary<string, Regex> AllowPatterns = new ConcurrentDictionary<string, Regex>(StringComparer.Ordinal);

    /// <summary>True when any anchor line, or the comment lines directly above any anchor, carries an allow comment for the rule.</summary>
    /// <param name="lines">The file split with <see cref="TextLines.Split"/>, so line numbers agree with the parsers'.</param>
    /// <param name="anchorLines">1-based lines; out-of-range anchors are ignored.</param>
    public static bool IsSuppressed(IReadOnlyList<string> lines, RuleDescriptor rule, params int[] anchorLines)
    {
        var pattern = AllowPattern(rule);

        foreach (var anchor in anchorLines.Distinct())
        {
            if (anchor < 1 || anchor > lines.Count)
            {
                continue;
            }

            if (pattern.IsMatch(lines[anchor - 1]))
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

    // One pattern per rule, built on first use. The reason must start with something other than '-' or '>',
    // otherwise a bare "allow hex -->" would count.
    private static Regex AllowPattern(RuleDescriptor rule) =>
        AllowPatterns.GetOrAdd(rule.SuppressionKeyword, keyword =>
            new Regex(@"(?:uno|xaml)-lint:\s*allow\s+(?:" + Regex.Escape(keyword) + @"|all)\s*-\s*[^\s\->]", RegexOptions.IgnoreCase));
}

/// <summary>
/// Line splitting shared by every rule and host. CRLF, lone LF and lone CR all end a line, as they do for XmlReader
/// and Roslyn, so a finding's line number points at the same line whichever component produced it.
/// </summary>
public static class TextLines
{
    public static string[] Split(string text) => Normalize(text).Split('\n');

    /// <summary>The text with every line ending as a lone LF.</summary>
    public static string Normalize(string text) => text.IndexOf('\r') < 0 ? text : text.Replace("\r\n", "\n").Replace('\r', '\n');

    /// <summary>Number of lines, without allocating them.</summary>
    public static int Count(string text)
    {
        var count = 1;
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (c == '\n' || (c == '\r' && (i + 1 == text.Length || text[i + 1] != '\n')))
            {
                count++;
            }
        }

        return count;
    }
}
