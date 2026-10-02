namespace Uno.Lint;

/// <summary>One hunk of a unified diff: the 1-based line it starts at in the new file and its prefixed lines (" ", "+", "-").</summary>
public sealed class PatchHunk
{
    public PatchHunk(int newStart, IReadOnlyList<string> lines)
    {
        NewStart = newStart;
        Lines = lines;
    }

    public int NewStart { get; }
    public IReadOnlyList<string> Lines { get; }
}

/// <summary>Finds the lines an edit added in the saved file, so a hook reports only what the agent just wrote.</summary>
public static class EditRange
{
    // Above this many old x new line pairs the diff is skipped and every new line counts as added.
    private const long MaxDiffCells = 4_000_000;

    /// <summary>
    /// The 1-based lines added by a structured patch (Claude Code's tool_response.structuredPatch). This is exact:
    /// it covers replace-all, copied blocks and short edits, so prefer it whenever the host provides it.
    /// </summary>
    public static HashSet<int> FromPatch(IEnumerable<PatchHunk> hunks)
    {
        var added = new HashSet<int>();
        foreach (var hunk in hunks)
        {
            var line = hunk.NewStart;
            foreach (var raw in hunk.Lines)
            {
                if (raw.Length == 0 || raw[0] == ' ')
                {
                    line++;
                }
                else if (raw[0] == '+')
                {
                    added.Add(line++);
                }

                // '-' lines are not in the new file; '\' is "No newline at end of file".
            }
        }

        return added;
    }

    /// <summary>Every line of the file, for a file the edit created or rewrote whole.</summary>
    public static HashSet<int> AllLines(string fullText) =>
        new HashSet<int>(Enumerable.Range(1, Normalize(fullText).Split('\n').Length));

    /// <summary>
    /// Fallback for hosts without a structured patch: diffs <paramref name="oldString"/> against
    /// <paramref name="newString"/> to find the lines that are new, then locates <paramref name="newString"/> in the
    /// saved file ignoring line-ending differences, or failing that ignoring whitespace. Returns false when the text is
    /// not found, or found more than once without <paramref name="replaceAll"/>: a guess would blame the wrong lines.
    /// </summary>
    public static bool TryLocate(string fullText, string oldString, string newString, bool replaceAll, out HashSet<int> lines)
    {
        lines = new HashSet<int>();
        if (string.IsNullOrEmpty(fullText) || string.IsNullOrEmpty(newString))
        {
            return false;
        }

        var full = Normalize(fullText);
        var needle = Normalize(newString);
        var newLines = needle.Split('\n');
        var addedOffsets = AddedLineOffsets(Normalize(oldString ?? string.Empty).Split('\n'), newLines);

        var starts = AllIndexesOf(full, needle);
        if (starts.Count > 0)
        {
            if (starts.Count > 1 && !replaceAll)
            {
                return false;
            }

            foreach (var start in starts)
            {
                var first = LineAt(full, start);
                foreach (var offset in addedOffsets)
                {
                    lines.Add(first + offset);
                }
            }

            return true;
        }

        return TryLocateIgnoringWhitespace(full, newLines, addedOffsets, replaceAll, lines);
    }

    /// <summary>0-based indexes of the lines in <paramref name="newLines"/> that are not part of a longest common subsequence with <paramref name="oldLines"/>.</summary>
    private static List<int> AddedLineOffsets(string[] oldLines, string[] newLines)
    {
        var n = oldLines.Length;
        var m = newLines.Length;
        if ((long)n * m > MaxDiffCells)
        {
            return Enumerable.Range(0, m).ToList();
        }

        // lcs[i, j] = LCS length of oldLines[i..] and newLines[j..]
        var lcs = new int[n + 1, m + 1];
        for (var i = n - 1; i >= 0; i--)
        {
            for (var j = m - 1; j >= 0; j--)
            {
                lcs[i, j] = oldLines[i] == newLines[j] ? lcs[i + 1, j + 1] + 1 : Math.Max(lcs[i + 1, j], lcs[i, j + 1]);
            }
        }

        var added = new List<int>();
        int a = 0, b = 0;
        while (b < m)
        {
            if (a < n && oldLines[a] == newLines[b])
            {
                a++;
                b++;
            }
            else if (a < n && lcs[a + 1, b] >= lcs[a, b + 1])
            {
                a++;
            }
            else
            {
                added.Add(b++);
            }
        }

        return added;
    }

    private static bool TryLocateIgnoringWhitespace(string full, string[] newLines, List<int> addedOffsets, bool replaceAll, HashSet<int> lines)
    {
        // Compact both sides, remembering where each kept character of the file came from.
        var map = new List<int>(full.Length);
        var compact = new System.Text.StringBuilder(full.Length);
        for (var i = 0; i < full.Length; i++)
        {
            if (!char.IsWhiteSpace(full[i]))
            {
                compact.Append(full[i]);
                map.Add(i);
            }
        }

        // Compact offset at which each needle line's first non-whitespace character lands (-1 for blank lines).
        var lineStarts = new int[newLines.Length];
        var compactNeedle = new System.Text.StringBuilder();
        for (var i = 0; i < newLines.Length; i++)
        {
            var kept = new string(newLines[i].Where(c => !char.IsWhiteSpace(c)).ToArray());
            lineStarts[i] = kept.Length == 0 ? -1 : compactNeedle.Length;
            compactNeedle.Append(kept);
        }

        if (compactNeedle.Length == 0)
        {
            return false;
        }

        var starts = AllIndexesOf(compact.ToString(), compactNeedle.ToString());
        if (starts.Count == 0 || (starts.Count > 1 && !replaceAll))
        {
            return false;
        }

        foreach (var at in starts)
        {
            foreach (var offset in addedOffsets)
            {
                if (lineStarts[offset] >= 0)
                {
                    lines.Add(LineAt(full, map[at + lineStarts[offset]]));
                }
            }
        }

        return true;
    }

    private static List<int> AllIndexesOf(string haystack, string needle)
    {
        var found = new List<int>();
        for (var at = haystack.IndexOf(needle, StringComparison.Ordinal); at >= 0; at = haystack.IndexOf(needle, at + needle.Length, StringComparison.Ordinal))
        {
            found.Add(at);
        }

        return found;
    }

    private static string Normalize(string text) => text.Replace("\r\n", "\n").Replace('\r', '\n');

    private static int LineAt(string text, int index)
    {
        var line = 1;
        for (var i = 0; i < index && i < text.Length; i++)
        {
            if (text[i] == '\n')
            {
                line++;
            }
        }

        return line;
    }
}
