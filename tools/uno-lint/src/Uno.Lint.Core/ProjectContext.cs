using System.Text.RegularExpressions;

namespace Uno.Lint;

/// <summary>What the project references, so rules only suggest controls and resources the project can actually use. Immutable.</summary>
public sealed class ProjectContext
{
    private static readonly Regex ToolkitXmlns = new Regex(@"using:Uno\.Toolkit\.UI\b");
    private static readonly Regex NavigationXmlns = new Regex(@"using:Uno\.Extensions\.Navigation(\.UI)?\b");
    private static readonly Regex MvuxXmlns = new Regex(@"using:Uno\.Extensions\.Reactive(\.UI)?\b");
    private static readonly Regex ThemesXmlns = new Regex(@"using:Uno\.(Material|Cupertino|Simple|Themes)\b");
    // Space*/Radius* tokens come from Uno.Themes BaseTheme, which MaterialTheme and SimpleTheme inherit. Cupertino is left out: unverified.
    private static readonly Regex TokensXmlns = new Regex(@"using:Uno\.(Material|Simple|Themes)\b");
    // Attributes allowed: the Uno template and many apps write <UnoFeatures Condition="...">.
    private static readonly Regex UnoFeatures = new Regex(@"<UnoFeatures\b[^>]*>([^<]*)</UnoFeatures>", RegexOptions.IgnoreCase);
    private static readonly Regex XmlComment = new Regex(@"<!--.*?-->", RegexOptions.Singleline);
    private static readonly Regex ToolkitPackage = new Regex(@"PackageReference\s+Include\s*=\s*""Uno\.Toolkit", RegexOptions.IgnoreCase);
    private static readonly Regex NavigationPackage = new Regex(@"PackageReference\s+Include\s*=\s*""Uno\.Extensions\.Navigation", RegexOptions.IgnoreCase);
    private static readonly Regex MvuxPackage = new Regex(@"PackageReference\s+Include\s*=\s*""Uno\.Extensions\.Reactive", RegexOptions.IgnoreCase);
    private static readonly Regex ThemesPackage = new Regex(@"PackageReference\s+Include\s*=\s*""Uno\.(Material|Cupertino|Simple|Themes)\b", RegexOptions.IgnoreCase);
    private static readonly Regex TokensPackage = new Regex(@"PackageReference\s+Include\s*=\s*""Uno\.(Material|Simple|Themes)\b", RegexOptions.IgnoreCase);
    private static readonly Regex FeatureSplit = new Regex(@"[;\s]+");

    /// <summary>No project, or one that references none of the libraries the rules know about.</summary>
    public static readonly ProjectContext Empty = new ProjectContext();

    public ProjectContext(bool hasToolkit = false, bool hasExtensionsNavigation = false, bool hasMvux = false, bool hasUnoThemes = false, bool hasThemeTokens = false)
    {
        HasToolkit = hasToolkit;
        HasExtensionsNavigation = hasExtensionsNavigation;
        HasMvux = hasMvux;
        HasUnoThemes = hasUnoThemes;
        HasThemeTokens = hasThemeTokens;
    }

    public bool HasToolkit { get; }

    public bool HasExtensionsNavigation { get; }

    /// <summary>Uno.Extensions.Reactive (MVUX) is referenced; enables the MVUX binding-trap rules.</summary>
    public bool HasMvux { get; }

    /// <summary>Uno.Themes (Material, Cupertino or SimpleTheme) is referenced, so role brushes like PrimaryBrush exist.</summary>
    public bool HasUnoThemes { get; }

    /// <summary>A Material or Simple theme is referenced, so the Uno.Themes Space* and Radius* tokens exist at runtime.</summary>
    public bool HasThemeTokens { get; }

    /// <summary>True when every flag is already set, so no further evidence can change the result.</summary>
    public bool IsComplete => HasToolkit && HasExtensionsNavigation && HasMvux && HasUnoThemes && HasThemeTokens;

    public ProjectContext WithToolkit(bool hasToolkit) => new ProjectContext(hasToolkit, HasExtensionsNavigation, HasMvux, HasUnoThemes, HasThemeTokens);

    /// <summary>What either context references: a run over several projects reports the libraries any of them uses.</summary>
    public ProjectContext Union(ProjectContext other) =>
        new ProjectContext(HasToolkit || other.HasToolkit, HasExtensionsNavigation || other.HasExtensionsNavigation, HasMvux || other.HasMvux, HasUnoThemes || other.HasUnoThemes, HasThemeTokens || other.HasThemeTokens);

    /// <summary>Detects the context from project files (csproj, Directory.Build.props...) and XAML texts.</summary>
    public static ProjectContext Detect(IEnumerable<string> projectFileTexts, IEnumerable<string> xamlTexts) =>
        FromProjectFiles(projectFileTexts).AddXaml(xamlTexts);

    public static ProjectContext FromProjectFiles(IEnumerable<string> projectFileTexts)
    {
        bool toolkit = false, navigation = false, mvux = false, themes = false, tokens = false;
        foreach (var raw in projectFileTexts)
        {
            // A commented-out reference or feature list is not a reference.
            var text = raw.IndexOf("<!--", StringComparison.Ordinal) >= 0 ? XmlComment.Replace(raw, string.Empty) : raw;
            toolkit |= ToolkitPackage.IsMatch(text);
            navigation |= NavigationPackage.IsMatch(text);
            mvux |= MvuxPackage.IsMatch(text);
            themes |= ThemesPackage.IsMatch(text);
            tokens |= TokensPackage.IsMatch(text);

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
                        case "simpletheme":
                        case "themes":
                            themes = true;
                            tokens = true;
                            break;
                        case "cupertino":
                            themes = true;
                            break;
                    }

                    // Composite features such as MaterialToolkit also bring the theme.
                    if (feature.IndexOf("Material", StringComparison.OrdinalIgnoreCase) >= 0 || feature.IndexOf("Cupertino", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        themes = true;
                    }

                    if (feature.IndexOf("Material", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        tokens = true;
                    }
                }
            }
        }

        return new ProjectContext(toolkit, navigation, mvux, themes, tokens);
    }

    /// <summary>This context plus the flags the XAML namespaces show; stops reading once every flag is set.</summary>
    public ProjectContext AddXaml(IEnumerable<string> xamlTexts)
    {
        bool toolkit = HasToolkit, navigation = HasExtensionsNavigation, mvux = HasMvux, themes = HasUnoThemes, tokens = HasThemeTokens;
        foreach (var text in xamlTexts)
        {
            if (toolkit && navigation && mvux && themes && tokens)
            {
                break;
            }

            toolkit |= ToolkitXmlns.IsMatch(text);
            navigation |= NavigationXmlns.IsMatch(text);
            mvux |= MvuxXmlns.IsMatch(text);
            themes |= ThemesXmlns.IsMatch(text);
            tokens |= TokensXmlns.IsMatch(text);
        }

        return new ProjectContext(toolkit, navigation, mvux, themes, tokens);
    }

    /// <summary>Compact form for the on-disk cache: one character per flag.</summary>
    public string Serialize() => $"{(HasToolkit ? 'T' : '-')}{(HasExtensionsNavigation ? 'N' : '-')}{(HasMvux ? 'M' : '-')}{(HasUnoThemes ? 'H' : '-')}{(HasThemeTokens ? 'S' : '-')}";

    public static bool TryDeserialize(string? text, out ProjectContext context)
    {
        context = Empty;
        if (text == null || text.Length != 5)
        {
            return false;
        }

        context = new ProjectContext(text[0] == 'T', text[1] == 'N', text[2] == 'M', text[3] == 'H', text[4] == 'S');
        return true;
    }
}
