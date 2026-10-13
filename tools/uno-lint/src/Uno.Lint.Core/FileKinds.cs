using System.Text.RegularExpressions;

namespace Uno.Lint;

/// <summary>
/// File-name based roles shared by the XAML and C# rules, matched against the file name's words
/// ("BrandColors.Dark.xaml" -> "Brand Colors Dark"), so a page whose name merely contains the word is not exempt:
/// ColorPickerPage, AuthTokenPage and LexiconPage are linted like any page.
/// </summary>
public static class FileKinds
{
    private static readonly Regex PaletteName = new Regex(@"(?i)\b(palettes?|colou?rs|tokens)\b", RegexOptions.Compiled);
    private static readonly Regex IconName = new Regex(@"(?i)\bicons\b", RegexOptions.Compiled);
    private static readonly Regex ThemeOverrideName = new Regex(@"(?i)\bcolou?r( palette)? override\b", RegexOptions.Compiled);

    /// <summary>A palette or token file: literal colors belong here, so HEX does not fire.</summary>
    public static bool IsPaletteFile(string path) => PaletteName.IsMatch(Words(path));

    public static bool IsIconFile(string path) => IconName.IsMatch(Words(path));

    public static bool IsThemeOverrideFile(string path) => ThemeOverrideName.IsMatch(Words(path));

    /// <summary>Only .xaml and .cs files are lintable; anything else is rejected rather than parsed as C#.</summary>
    public static bool IsSourceFile(string path) =>
        path.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase);

    private static string Words(string path)
    {
        var stem = Path.GetFileName(path);
        // "App.xaml.cs" -> "App"; "Palette.cs" -> "Palette"
        while (true)
        {
            var next = Path.GetFileNameWithoutExtension(stem);
            if (next == stem || next.Length == 0)
            {
                break;
            }

            stem = next;
        }

        return string.Join(" ", stem.Split('.', '-', ' ').Select(CSharpAnalyzer.SplitWords));
    }
}

/// <summary>Recursive file walk that never descends into bin, obj, .git and the like.</summary>
public static class FileWalker
{
    private static readonly HashSet<string> ExcludedDirNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "bin", "obj", "node_modules", ".git", ".vs", ".idea" };

    public static IEnumerable<string> Enumerate(string root, Func<string, bool> accept)
    {
        var pending = new Stack<string>();
        pending.Push(root);
        while (pending.Count > 0)
        {
            var dir = pending.Pop();
            IEnumerable<string> entries;
            try
            {
                entries = Directory.EnumerateFileSystemEntries(dir);
            }
            catch (IOException)
            {
                continue;
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }

            foreach (var entry in entries)
            {
                if (Directory.Exists(entry))
                {
                    if (!ExcludedDirNames.Contains(Path.GetFileName(entry)))
                    {
                        pending.Push(entry);
                    }
                }
                else if (accept(entry))
                {
                    yield return entry;
                }
            }
        }
    }
}
