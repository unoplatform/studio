namespace Uno.Lint;

/// <summary>Locates the lines an edit added inside the saved file, so a hook reports only what the agent just wrote.</summary>
public static class EditRange
{
    /// <summary>
    /// Finds <paramref name="added"/> inside <paramref name="fullText"/> ignoring line-ending differences and
    /// returns the 1-based inclusive line range it occupies. Falls back to a whitespace-insensitive search
    /// when the exact text is not found (editors may re-indent).
    /// </summary>
    public static bool TryLocate(string fullText, string added, out int firstLine, out int lastLine)
    {
        firstLine = lastLine = 0;
        if (string.IsNullOrEmpty(fullText) || string.IsNullOrEmpty(added))
        {
            return false;
        }

        var full = Normalize(fullText);
        var needle = Normalize(added).Trim('\n');
        if (needle.Length == 0)
        {
            return false;
        }

        var start = full.IndexOf(needle, StringComparison.Ordinal);
        if (start < 0)
        {
            // Retry with collapsed whitespace: map back to the original by walking both strings.
            start = IndexOfIgnoringWhitespace(full, needle, out var length);
            if (start < 0)
            {
                return false;
            }

            firstLine = LineAt(full, start);
            lastLine = LineAt(full, start + Math.Max(0, length - 1));
            return true;
        }

        firstLine = LineAt(full, start);
        lastLine = LineAt(full, start + needle.Length - 1);
        return true;
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

    private static int IndexOfIgnoringWhitespace(string haystack, string needle, out int matchedLength)
    {
        var compactNeedle = new string(needle.Where(c => !char.IsWhiteSpace(c)).ToArray());
        matchedLength = 0;
        if (compactNeedle.Length == 0)
        {
            return -1;
        }

        // Map compact positions back to original positions.
        var map = new List<int>(haystack.Length);
        var compact = new System.Text.StringBuilder(haystack.Length);
        for (var i = 0; i < haystack.Length; i++)
        {
            if (!char.IsWhiteSpace(haystack[i]))
            {
                compact.Append(haystack[i]);
                map.Add(i);
            }
        }

        var at = compact.ToString().IndexOf(compactNeedle, StringComparison.Ordinal);
        if (at < 0)
        {
            return -1;
        }

        var startOriginal = map[at];
        var endOriginal = map[at + compactNeedle.Length - 1];
        matchedLength = endOriginal - startOriginal + 1;
        return startOriginal;
    }
}
