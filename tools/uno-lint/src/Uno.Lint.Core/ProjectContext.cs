using System.Text.RegularExpressions;

namespace Uno.Lint;

/// <summary>What the project references, so rules only suggest controls and resources the project can actually use. Immutable.</summary>
public sealed class ProjectContext
{
    private static readonly Regex ToolkitXmlns = new Regex(@"using:Uno\.Toolkit\.UI\b");
    private static readonly Regex NavigationXmlns = new Regex(@"using:Uno\.Extensions\.Navigation(\.UI)?\b");
    private static readonly Regex MvuxXmlns = new Regex(@"using:Uno\.Extensions\.Reactive(\.UI)?\b");
    private static readonly Regex ThemesXmlns = new Regex(@"using:Uno\.(Material|Cupertino|Simple|Themes)\b");
    private static readonly Regex UnoFeatures = new Regex(@"<UnoFeatures>([^<]*)</UnoFeatures>", RegexOptions.IgnoreCase);
    private static readonly Regex ToolkitPackage = new Regex(@"PackageReference\s+Include\s*=\s*""Uno\.Toolkit", RegexOptions.IgnoreCase);
    private static readonly Regex NavigationPackage = new Regex(@"PackageReference\s+Include\s*=\s*""Uno\.Extensions\.Navigation", RegexOptions.IgnoreCase);
    private static readonly Regex MvuxPackage = new Regex(@"PackageReference\s+Include\s*=\s*""Uno\.Extensions\.Reactive", RegexOptions.IgnoreCase);
    private static readonly Regex ThemesPackage = new Regex(@"PackageReference\s+Include\s*=\s*""Uno\.(Material|Cupertino|Simple|Themes)\b", RegexOptions.IgnoreCase);
    private static readonly Regex FeatureSplit = new Regex(@"[;\s]+");

    /// <summary>No project, or one that references none of the libraries the rules know about.</summary>
    public static readonly ProjectContext Empty = new ProjectContext();

    public ProjectContext(bool hasToolkit = false, bool hasExtensionsNavigation = false, bool hasMvux = false, bool hasUnoThemes = false)
    {
        HasToolkit = hasToolkit;
        HasExtensionsNavigation = hasExtensionsNavigation;
        HasMvux = hasMvux;
        HasUnoThemes = hasUnoThemes;
    }

    public bool HasToolkit { get; }

    public bool HasExtensionsNavigation { get; }

    /// <summary>Uno.Extensions.Reactive (MVUX) is referenced; enables the MVUX binding-trap rules.</summary>
    public bool HasMvux { get; }

    /// <summary>Uno.Themes (Material, Cupertino or SimpleTheme) is referenced, so role brushes like PrimaryBrush exist.</summary>
    public bool HasUnoThemes { get; }

    /// <summary>True when every flag is already set, so no further evidence can change the result.</summary>
    public bool IsComplete => HasToolkit && HasExtensionsNavigation && HasMvux && HasUnoThemes;

    public ProjectContext WithToolkit(bool hasToolkit) => new ProjectContext(hasToolkit, HasExtensionsNavigation, HasMvux, HasUnoThemes);

    /// <summary>What either context references: a run over several projects reports the libraries any of them uses.</summary>
    public ProjectContext Union(ProjectContext other) =>
        new ProjectContext(HasToolkit || other.HasToolkit, HasExtensionsNavigation || other.HasExtensionsNavigation, HasMvux || other.HasMvux, HasUnoThemes || other.HasUnoThemes);

    /// <summary>Detects the context from project files (csproj, Directory.Build.props...) and XAML texts.</summary>
    public static ProjectContext Detect(IEnumerable<string> projectFileTexts, IEnumerable<string> xamlTexts) =>
        FromProjectFiles(projectFileTexts).AddXaml(xamlTexts);

    public static ProjectContext FromProjectFiles(IEnumerable<string> projectFileTexts)
    {
        bool toolkit = false, navigation = false, mvux = false, themes = false;
        foreach (var text in projectFileTexts)
        {
            toolkit |= ToolkitPackage.IsMatch(text);
            navigation |= NavigationPackage.IsMatch(text);
            mvux |= MvuxPackage.IsMatch(text);
            themes |= ThemesPackage.IsMatch(text);

            foreach (Match m in UnoFeatures.Matches(text))
            {
                foreach (var feature in FeatureSplit.Split(m.Groups[1].Value))
                {
                    switch (feature.Trim().ToLowerInvariant())
                    {
                        case "toolkit":
                        case "materialtoolkit":
                        case "cupertinotoolkit":
                            toolkit = true;
                            break;
                        case "navigation":
                            navigation = true;
                            break;
                        case "mvux":
                            mvux = true;
                            break;
                        case "material":
                        case "cupertino":
                        case "simpletheme":
                        case "themes":
                            themes = true;
                            break;
                    }

                    // Composite features such as MaterialToolkit also bring the theme.
                    if (feature.IndexOf("Material", StringComparison.OrdinalIgnoreCase) >= 0 || feature.IndexOf("Cupertino", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        themes = true;
                    }
                }
            }
        }

        return new ProjectContext(toolkit, navigation, mvux, themes);
    }

    /// <summary>This context plus the flags the XAML namespaces show; stops reading once every flag is set.</summary>
    public ProjectContext AddXaml(IEnumerable<string> xamlTexts)
    {
        bool toolkit = HasToolkit, navigation = HasExtensionsNavigation, mvux = HasMvux, themes = HasUnoThemes;
        foreach (var text in xamlTexts)
        {
            if (toolkit && navigation && mvux && themes)
            {
                break;
            }

            toolkit |= ToolkitXmlns.IsMatch(text);
            navigation |= NavigationXmlns.IsMatch(text);
            mvux |= MvuxXmlns.IsMatch(text);
            themes |= ThemesXmlns.IsMatch(text);
        }

        return new ProjectContext(toolkit, navigation, mvux, themes);
    }

    /// <summary>Compact form for the on-disk cache: one character per flag.</summary>
    public string Serialize() => $"{(HasToolkit ? 'T' : '-')}{(HasExtensionsNavigation ? 'N' : '-')}{(HasMvux ? 'M' : '-')}{(HasUnoThemes ? 'H' : '-')}";

    public static bool TryDeserialize(string? text, out ProjectContext context)
    {
        context = Empty;
        if (text == null || text.Length != 4)
        {
            return false;
        }

        context = new ProjectContext(text[0] == 'T', text[1] == 'N', text[2] == 'M', text[3] == 'H');
        return true;
    }
}
