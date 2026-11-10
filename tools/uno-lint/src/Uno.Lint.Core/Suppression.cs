using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Uno.Lint;

/// <summary>How a file writes comments and literals, for <see cref="Suppression"/>.</summary>
public enum CommentSyntax
{
    /// <summary>XAML: &lt;!-- --&gt; comments; a CDATA section is content. // and /* mean nothing.</summary>
    Xml,

    /// <summary>C#: // and /* */ comments; string and character literals (regular, verbatim, raw) are code.</summary>
    CSharp,
}

/// <summary>
/// A finding is suppressed by a comment on the same line or in the lines directly above it (up to three, blank lines
/// included), reason required:
///   &lt;!-- uno-lint: allow card - image clip with a placeholder fill --&gt;
///   // uno-lint: allow codebehind - video transport glue, no Toolkit equivalent
/// The look-behind stops at the first line that is not entirely comment or blank, so a comment never reaches past
/// another element or statement, even one with a trailing comment; the lines of a multi-line comment count as comment
/// lines. A comment opener inside a literal (<c>"image/*"</c>, <c>Text="/*"</c>) opens nothing.
/// For a finding on an attribute of a multi-line element, the element's start line is an anchor too: XML has no place
/// for a comment inside a start tag. "allow all - reason" covers every rule on that line.
/// The legacy "xaml-lint:" prefix from the PowerShell prototype is accepted too.
/// </summary>
public static class Suppression
{
    private const int LookBehindLines = 3;

    private static readonly ConcurrentDictionary<string, Regex> AllowPatterns = new ConcurrentDictionary<string, Regex>(StringComparer.Ordinal);

    // Which lines of a file are entirely comment, computed once per split file: callers pass the same lines for
    // every finding of a file. A file has one syntax, so one table per syntax.
    private static readonly ConditionalWeakTable<IReadOnlyList<string>, bool[]> XmlCommentOnlyLines = new ConditionalWeakTable<IReadOnlyList<string>, bool[]>();
    private static readonly ConditionalWeakTable<IReadOnlyList<string>, bool[]> CSharpCommentOnlyLines = new ConditionalWeakTable<IReadOnlyList<string>, bool[]>();
    private static readonly ConditionalWeakTable<IReadOnlyList<string>, bool[]>.CreateValueCallback ScanXml = lines => FindCommentOnlyLines(lines, CommentSyntax.Xml);
    private static readonly ConditionalWeakTable<IReadOnlyList<string>, bool[]>.CreateValueCallback ScanCSharp = lines => FindCommentOnlyLines(lines, CommentSyntax.CSharp);

    /// <summary>True when any anchor line, or the comment lines directly above any anchor, carries an allow comment for the rule.</summary>
    public static bool IsSuppressed(IReadOnlyList<string> lines, RuleDescriptor rule, CommentSyntax syntax, params int[] anchorLines) =>
        FindAllowComment(lines, rule, syntax, anchorLines) != null;

    /// <summary>The 1-based line of the allow comment for the rule on an anchor line or in the comment lines directly above one; null when there is none.</summary>
    /// <param name="lines">The file split with <see cref="TextLines.Split"/>, so line numbers agree with the parsers'.</param>
    /// <param name="syntax">How the file writes comments and literals, so a comment opener inside a literal is not taken for a comment.</param>
    /// <param name="anchorLines">1-based lines; out-of-range anchors are ignored.</param>
    public static int? FindAllowComment(IReadOnlyList<string> lines, RuleDescriptor rule, CommentSyntax syntax, params int[] anchorLines)
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
                return anchor;
            }

            // Blank lines count toward the three, so "directly above" stays three lines whatever the spacing.
            for (var i = anchor - 2; i >= 0 && i >= anchor - 1 - LookBehindLines; i--)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                commentOnly ??= syntax == CommentSyntax.Xml ? XmlCommentOnlyLines.GetValue(lines, ScanXml) : CSharpCommentOnlyLines.GetValue(lines, ScanCSharp);
                if (!commentOnly[i])
                {
                    break; // another element or statement: the comment above it is not ours
                }

                if (pattern.IsMatch(line))
                {
                    return i + 1;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Marks the lines that hold a comment and nothing else. Comments, CDATA sections and verbatim or raw strings are
    /// followed across lines; a regular string or character literal ends with its line, so an unterminated one cannot
    /// leak into the lines below.
    /// </summary>
    private static bool[] FindCommentOnlyLines(IReadOnlyList<string> lines, CommentSyntax syntax)
    {
        var result = new bool[lines.Count];
        var scanner = new LineScanner(syntax);
        for (var i = 0; i < lines.Count; i++)
        {
            result[i] = scanner.IsCommentOnly(lines[i]);
        }

        return result;
    }

    /// <summary>What a line holds, given what the lines above left open.</summary>
    private struct LineScanner
    {
        private readonly CommentSyntax _syntax;
        private Open _open;
        private int _rawQuotes;

        public LineScanner(CommentSyntax syntax) => _syntax = syntax;

        private enum Open
        {
            Nothing,
            Comment,
            Cdata,
            VerbatimString,
            RawString,
        }

        public bool IsCommentOnly(string line)
        {
            bool hasComment = _open == Open.Comment, hasCode = _open is Open.Cdata or Open.VerbatimString or Open.RawString;
            var at = 0;
            while (at < line.Length)
            {
                switch (_open)
                {
                    case Open.Comment:
                        at = Close(line, at, _syntax == CommentSyntax.Xml ? "-->" : "*/");
                        break;
                    case Open.Cdata:
                        at = Close(line, at, "]]>");
                        break;
                    case Open.RawString:
                        at = Close(line, at, new string('"', _rawQuotes));
                        break;
                    case Open.VerbatimString:
                        at = CloseVerbatim(line, at);
                        break;
                    default:
                        if (char.IsWhiteSpace(line[at]))
                        {
                            at++;
                        }
                        else if (_syntax == CommentSyntax.Xml)
                        {
                            at = ScanXml(line, at, ref hasComment, ref hasCode);
                        }
                        else if (string.CompareOrdinal(line, at, "//", 0, 2) == 0)
                        {
                            hasComment = true;
                            at = line.Length;
                        }
                        else
                        {
                            at = ScanCSharp(line, at, ref hasComment, ref hasCode);
                        }

                        break;
                }
            }

            return hasComment && !hasCode;
        }

        private int ScanXml(string line, int at, ref bool hasComment, ref bool hasCode)
        {
            if (string.CompareOrdinal(line, at, "<!--", 0, 4) == 0)
            {
                (_open, hasComment) = (Open.Comment, true);
                return at + 4;
            }

            hasCode = true;
            if (string.CompareOrdinal(line, at, "<![CDATA[", 0, 9) == 0)
            {
                _open = Open.Cdata;
                return at + 9;
            }

            return at + 1;
        }

        private int ScanCSharp(string line, int at, ref bool hasComment, ref bool hasCode)
        {
            if (string.CompareOrdinal(line, at, "/*", 0, 2) == 0)
            {
                (_open, hasComment) = (Open.Comment, true);
                return at + 2;
            }

            hasCode = true;
            if (line[at] == '\'')
            {
                return SkipQuoted(line, at + 1, '\'');
            }

            // A string, after its $ and @ prefixes: $"..", @"..", $@"..", """..""", $$"""..""".
            var quote = at;
            while (quote < line.Length && line[quote] is '$' or '@')
            {
                quote++;
            }

            if (quote == line.Length || line[quote] != '"')
            {
                return at + 1;
            }

            var quotes = 0;
            while (quote + quotes < line.Length && line[quote + quotes] == '"')
            {
                quotes++;
            }

            if (quotes >= 3)
            {
                (_open, _rawQuotes) = (Open.RawString, quotes);
                return quote + quotes;
            }

            if (line.IndexOf('@', at, quote - at) >= 0)
            {
                _open = Open.VerbatimString;
                return quote + 1;
            }

            return SkipQuoted(line, quote + 1, '"');
        }

        /// <summary>Past the closer when the line holds it (the construct is closed), else the end of the line.</summary>
        private int Close(string line, int at, string closer)
        {
            var end = line.IndexOf(closer, at, StringComparison.Ordinal);
            if (end < 0)
            {
                return line.Length;
            }

            _open = Open.Nothing;
            return end + closer.Length;
        }

        /// <summary>A verbatim string ends at a quote that is not doubled.</summary>
        private int CloseVerbatim(string line, int at)
        {
            for (var i = at; i < line.Length; i++)
            {
                if (line[i] != '"')
                {
                    continue;
                }

                if (i + 1 < line.Length && line[i + 1] == '"')
                {
                    i++;
                    continue;
                }

                _open = Open.Nothing;
                return i + 1;
            }

            return line.Length;
        }

        /// <summary>Past the closing quote of a regular string or character literal, skipping escapes; the end of the line when it has none.</summary>
        private static int SkipQuoted(string line, int at, char quote)
        {
            for (var i = at; i < line.Length; i++)
            {
                if (line[i] == '\\')
                {
                    i++;
                }
                else if (line[i] == quote)
                {
                    return i + 1;
                }
            }

            return line.Length;
        }
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
