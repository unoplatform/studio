using System.Text.RegularExpressions;

namespace Uno.Lint;

/// <summary>
/// File-name based roles shared by the XAML and C# rules, matched against the file name's words
/// ("BrandColors.Dark.xaml" -> "Brand Colors Dark"), so a page whose name merely contains the word is not exempt:
/// ColorPickerPage, AuthTokenPage and LexiconPage are linted like any page.
/// </summary>
public static class FileKinds
{
    private static readonly Regex PaletteName = new Regex(@"(?i)\b(palettes?|colou?rs|tokens)\b");
    private static readonly Regex IconName = new Regex(@"(?i)\bicons\b");
    private static readonly Regex ThemeOverrideName = new Regex(@"(?i)\bcolou?r( palette)? override\b");

    /// <summary>A palette or token file: literal colors belong here, so HEX does not fire.</summary>
    public static bool IsPaletteFile(string path) => PaletteName.IsMatch(Words(path));

    public static bool IsIconFile(string path) => IconName.IsMatch(Words(path));

    public static bool IsThemeOverrideFile(string path) => ThemeOverrideName.IsMatch(Words(path));

    /// <summary>Only .xaml and .cs files are lintable; anything else is rejected rather than parsed as C#.</summary>
    public static bool IsSourceFile(string path) => IsXaml(path) || path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase);

    public static bool IsXaml(string path) => path.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase);

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
