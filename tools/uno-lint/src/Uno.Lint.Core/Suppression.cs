using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Uno.Lint;

/// <summary>
/// A finding is suppressed by a comment on the same line or in the comment lines directly above it (up to three),
/// reason required:
///   &lt;!-- uno-lint: allow card - image clip with a placeholder fill --&gt;
///   // uno-lint: allow codebehind - video transport glue, no Toolkit equivalent
/// The look-behind stops at the first line that is not entirely comment, so a comment never reaches past another
/// element or statement, even one with a trailing comment; the lines of a multi-line comment count as comment lines.
/// For a finding on an attribute of a multi-line element, the element's start line is an anchor too: XML has no place
/// for a comment inside a start tag. "allow all - reason" covers every rule on that line.
/// The legacy "xaml-lint:" prefix from the PowerShell prototype is accepted too.
/// </summary>
public static class Suppression
{
    private const int LookBehindLines = 3;

    private static readonly ConcurrentDictionary<string, Regex> AllowPatterns = new ConcurrentDictionary<string, Regex>(StringComparer.Ordinal);

    // Which lines of a file are entirely comment, computed once per split file: callers pass the same lines for
    // every finding of a file.
    private static readonly ConditionalWeakTable<IReadOnlyList<string>, bool[]> CommentOnlyLines = new ConditionalWeakTable<IReadOnlyList<string>, bool[]>();

    /// <summary>True when any anchor line, or the comment lines directly above any anchor, carries an allow comment for the rule.</summary>
    /// <param name="lines">The file split with <see cref="TextLines.Split"/>, so line numbers agree with the parsers'.</param>
    /// <param name="anchorLines">1-based lines; out-of-range anchors are ignored.</param>
    public static bool IsSuppressed(IReadOnlyList<string> lines, RuleDescriptor rule, params int[] anchorLines)
    {
        var pattern = AllowPattern(rule);
        bool[]? commentOnly = null;

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

                commentOnly ??= CommentOnlyLines.GetValue(lines, FindCommentOnlyLines);
                if (!commentOnly[i])
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

    /// <summary>
    /// Marks the lines that hold a comment and nothing else, tracking XML (&lt;!-- --&gt;) and C# (/* */) comments
    /// across lines; // runs to the end of its line. One scanner for both languages: a comment opener inside an
    /// attribute or string only matters when the line has no code before it, which a real element or statement has.
    /// </summary>
    private static bool[] FindCommentOnlyLines(IReadOnlyList<string> lines)
    {
        var result = new bool[lines.Count];
        string? closer = null;
        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            bool hasComment = closer != null, hasCode = false;
            var at = 0;
            while (at < line.Length)
            {
                if (closer != null)
                {
                    var end = line.IndexOf(closer, at, StringComparison.Ordinal);
                    at = end < 0 ? line.Length : end + closer.Length;
                    closer = end < 0 ? closer : null;
                }
                else if (char.IsWhiteSpace(line[at]))
                {
                    at++;
                }
                else if (string.CompareOrdinal(line, at, "<!--", 0, 4) == 0)
                {
                    (closer, hasComment, at) = ("-->", true, at + 4);
                }
                else if (string.CompareOrdinal(line, at, "/*", 0, 2) == 0)
                {
                    (closer, hasComment, at) = ("*/", true, at + 2);
                }
                else if (string.CompareOrdinal(line, at, "//", 0, 2) == 0)
                {
                    hasComment = true;
                    break;
                }
                else
                {
                    hasCode = true;
                    at++;
                }
            }

            result[i] = hasComment && !hasCode;
        }

        return result;
    }

    // One pattern per rule, built on first use. The reason must start with something other than '-' or '>',
    // otherwise a bare "allow hex -->" would count.
    private static Regex AllowPattern(RuleDescriptor rule) =>
        AllowPatterns.GetOrAdd(rule.SuppressionKeyword, keyword =>
            new Regex(@"(?:uno|xaml)-lint:\s*allow\s+(?:" + Regex.Escape(keyword) + @"|all)\s*-\s*[^\s\->]", RegexOptions.IgnoreCase));
}

/// <summary>
/// Line splitting shared by every rule and host. CRLF, lone LF and lone CR all end a line, as they do for XmlReader
/// and Roslyn, so a finding's line number points at the same line whichever component produced it. Roslyn also ends
/// a line at NEL, LS and PS, which XML does not: C# files take their lines from Roslyn (<c>CSharpFile.Lines</c>), and
/// <see cref="ToCSharpLines"/> converts line numbers counted here.
/// </summary>
public static class TextLines
{
    public static string[] Split(string text) => Normalize(text).Split('\n');

    /// <summary>The text with every line ending as a lone LF.</summary>
    public static string Normalize(string text) => text.IndexOf('\r') < 0 ? text : text.Replace("\r\n", "\n").Replace('\r', '\n');

    /// <summary>
    /// The C# line numbers covering <paramref name="lines"/>, which were counted with CR and LF only, as an edit payload
    /// counts them. Roslyn also ends a line at NEL, LS and PS, so one such line can span several C# lines.
    /// </summary>
    public static HashSet<int> ToCSharpLines(string text, IEnumerable<int> lines)
    {
        // first[n] and last[n]: the C# lines on which CR/LF line n starts and ends. Index 0 is unused.
        var first = new List<int> { 0, 1 };
        var last = new List<int> { 0 };
        var csharpLine = 1;
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (c == '\r' || c == '\n')
            {
                if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                {
                    i++;
                }

                last.Add(csharpLine++);
                first.Add(csharpLine);
            }
            else if (c == (char)0x85 || c == (char)0x2028 || c == (char)0x2029)
            {
                csharpLine++;
            }
        }

        last.Add(csharpLine);
        var result = new HashSet<int>();
        foreach (var line in lines)
        {
            for (var n = line >= 1 && line < first.Count ? first[line] : 1; line >= 1 && line < first.Count && n <= last[line]; n++)
            {
                result.Add(n);
            }
        }

        return result;
    }

    /// <summary>True when the text has a line break Roslyn honors and XML does not: NEL, LS or PS.</summary>
    public static bool HasUnicodeLineBreaks(string text) => text.IndexOf((char)0x85) >= 0 || text.IndexOf((char)0x2028) >= 0 || text.IndexOf((char)0x2029) >= 0;

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
