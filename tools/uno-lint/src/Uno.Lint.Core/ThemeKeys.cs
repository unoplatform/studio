using System.Reflection;

namespace Uno.Lint;

/// <summary>
/// The lightweight-styling keys each theme's control styles define or read, for THEMEKEY (UNOL202). Generated from the
/// theme packages by catalog/Update-ThemeKeys.ps1 and embedded; a key's family is the longest name in controls.txt it
/// starts with, followed by an upper-case letter (ListViewItemBackgroundSelected belongs to ListViewItem).
/// </summary>
public static class ThemeKeys
{
    private static readonly Lazy<Catalogs> Loaded = new Lazy<Catalogs>(Load);

    /// <summary>
    /// Why <paramref name="key"/> overrides nothing under <paramref name="context"/>'s themes, or null when a style the
    /// app gets for that control reads it, or when the key is not a control family's key at all, or no catalog knows the
    /// family (the rule then has nothing to compare with).
    /// </summary>
    public static ThemeKeyMiss? Check(string key, ProjectContext context) => Check(key, context, usesWinUIStyle: false);

    /// <param name="usesWinUIStyle">
    /// The file bases a style on WinUI's own style for the family (BasedOn DefaultButtonStyle), so WinUI's template
    /// reads its keys whatever the theme.
    /// </param>
    public static ThemeKeyMiss? Check(string key, ProjectContext context, bool usesWinUIStyle)
    {
        var catalogs = Loaded.Value;
        if (FamilyOf(key, catalogs.Families) is not { } family)
        {
            return null;
        }

        // The style the app gets for this family. An Uno theme replaces WinUI's template when it reads key names WinUI
        // does not have (Simple's ListViewItemBackgroundPointerOverSelected); a theme that only sets some of WinUI's
        // keys (Simple's NavigationViewItem sets a Foreground) leaves WinUI's template reading the rest. A project that
        // references a theme of unknown kind could be on any of them, and keeps WinUI's keys too.
        var readers = new List<Catalog>();
        var replaced = false;
        if (context.HasUnoThemes)
        {
            foreach (var theme in catalogs.Themes)
            {
                if ((context.Themes == UnoThemeKinds.None || (context.Themes & theme.Kind) != 0) && theme.Styles(family))
                {
                    readers.Add(theme);
                    replaced |= theme.Keys.Any(k => FamilyOf(k, catalogs.Families) == family && !catalogs.Fluent.Keys.Contains(k));
                }
            }
        }

        if (!replaced || context.Themes == UnoThemeKinds.None || usesWinUIStyle)
        {
            readers.Add(catalogs.Fluent);
        }

        if (!readers.Any(t => t.Styles(family)) || readers.Any(t => t.Keys.Contains(key)))
        {
            return null;
        }

        return new ThemeKeyMiss(family, readers[0].Display, Closest(key, family, readers));
    }

    /// <summary>The longest control family name <paramref name="key"/> starts with, followed by an upper-case letter.</summary>
    public static string? FamilyOf(string key) => FamilyOf(key, Loaded.Value.Families);

    private static string? FamilyOf(string key, IReadOnlyList<string> familiesLongestFirst)
    {
        foreach (var family in familiesLongestFirst)
        {
            if (key.Length > family.Length && key.StartsWith(family, StringComparison.Ordinal) && char.IsUpper(key[family.Length]))
            {
                return family;
            }
        }

        return null;
    }

    /// <summary>
    /// The real key the author most likely meant: the same words in another order first
    /// (BackgroundSelectedPointerOver for BackgroundPointerOverSelected), then the nearest by edit distance.
    /// </summary>
    private static string? Closest(string key, string family, IEnumerable<Catalog> themes)
    {
        var candidates = themes.SelectMany(t => t.Keys).Where(k => k.StartsWith(family, StringComparison.Ordinal) && FamilyOf(k) == family).Distinct().ToList();
        var words = Words(key.Substring(family.Length));
        var reordered = candidates.FirstOrDefault(c => Words(c.Substring(family.Length)).SequenceEqual(words));
        if (reordered != null)
        {
            return reordered;
        }

        var best = candidates.Select(c => (Key: c, Distance: Distance(key, c))).OrderBy(c => c.Distance).ThenBy(c => c.Key, StringComparer.Ordinal).FirstOrDefault();
        if (best.Key != null && best.Distance <= Math.Max(3, key.Length / 6))
        {
            return best.Key;
        }

        // Uno.Themes styles a control through variants: Material's default Button reads FilledButtonBackground, not
        // ButtonBackground. Same rest of the key, on a variant family that ends with this one.
        var rest = key.Substring(family.Length);
        return themes.SelectMany(t => t.Keys)
            .Where(k => k.EndsWith(family + rest, StringComparison.Ordinal) && k.Length > key.Length && FamilyOf(k) is { } f && f != family && f.EndsWith(family, StringComparison.Ordinal))
            .OrderBy(k => k.StartsWith("Filled", StringComparison.Ordinal) ? 0 : 1) // the themes' default variant
            .ThenBy(k => k.Length).ThenBy(k => k, StringComparer.Ordinal).FirstOrDefault();
    }

    private static IEnumerable<string> Words(string pascal)
    {
        var words = new List<string>();
        var start = 0;
        for (var i = 1; i <= pascal.Length; i++)
        {
            if (i == pascal.Length || char.IsUpper(pascal[i]))
            {
                words.Add(pascal.Substring(start, i - start));
                start = i;
            }
        }

        words.Sort(StringComparer.Ordinal);
        return words;
    }

    private static int Distance(string a, string b)
    {
        var previous = new int[b.Length + 1];
        var current = new int[b.Length + 1];
        for (var j = 0; j <= b.Length; j++)
        {
            previous[j] = j;
        }

        for (var i = 1; i <= a.Length; i++)
        {
            current[0] = i;
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1), previous[j - 1] + cost);
            }

            (previous, current) = (current, previous);
        }

        return previous[b.Length];
    }

    private static Catalogs Load()
    {
        var families = Lines("controls").OrderByDescending(f => f.Length).ThenBy(f => f, StringComparer.Ordinal).ToList();
        Catalog Read(string name, string display, UnoThemeKinds kind) => new Catalog(display, kind, new HashSet<string>(Lines(name), StringComparer.Ordinal), families);
        return new Catalogs(
            families,
            Read("fluent", "Uno's Fluent", UnoThemeKinds.None),
            new[]
            {
                Read("material", "Material's", UnoThemeKinds.Material),
                Read("cupertino", "Cupertino's", UnoThemeKinds.Cupertino),
                Read("simple", "Simple's", UnoThemeKinds.Simple),
            });
    }

    private static IEnumerable<string> Lines(string name)
    {
        using var stream = typeof(ThemeKeys).GetTypeInfo().Assembly.GetManifestResourceStream($"Uno.Lint.ThemeKeys.{name}.txt")
            ?? throw new InvalidOperationException($"Embedded catalog {name}.txt is missing.");
        using var reader = new StreamReader(stream);
        var lines = new List<string>();
        while (reader.ReadLine() is { } line)
        {
            line = line.Trim();
            if (line.Length > 0 && line[0] != '#')
            {
                lines.Add(line);
            }
        }

        return lines;
    }

    private sealed class Catalogs
    {
        public Catalogs(IReadOnlyList<string> families, Catalog fluent, IReadOnlyList<Catalog> themes)
        {
            Families = families;
            Fluent = fluent;
            Themes = themes;
        }

        public IReadOnlyList<string> Families { get; }
        public Catalog Fluent { get; }
        public IReadOnlyList<Catalog> Themes { get; }
    }

    private sealed class Catalog
    {
        private readonly HashSet<string> _families;

        public Catalog(string display, UnoThemeKinds kind, HashSet<string> keys, IReadOnlyList<string> familiesLongestFirst)
        {
            Display = display;
            Kind = kind;
            Keys = keys;
            _families = new HashSet<string>(keys.Select(k => FamilyOf(k, familiesLongestFirst)).OfType<string>(), StringComparer.Ordinal);
        }

        public string Display { get; }
        public UnoThemeKinds Kind { get; }
        public HashSet<string> Keys { get; }

        /// <summary>The theme's styles touch this family: at least one of its keys belongs to it.</summary>
        public bool Styles(string family) => _families.Contains(family);
    }
}

/// <summary>A lightweight-styling key no style the app gets reads, and the nearest key that style does read.</summary>
public sealed class ThemeKeyMiss
{
    public ThemeKeyMiss(string family, string theme, string? suggestion)
    {
        Family = family;
        Theme = theme;
        Suggestion = suggestion;
    }

    /// <summary>The control family, such as ListViewItem.</summary>
    public string Family { get; }

    /// <summary>Whose style it is, as a phrase: "Simple's", "Uno's Fluent".</summary>
    public string Theme { get; }

    public string? Suggestion { get; }
}
