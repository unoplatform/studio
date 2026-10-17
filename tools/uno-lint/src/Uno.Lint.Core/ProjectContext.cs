using System.Text.RegularExpressions;

namespace Uno.Lint;

/// <summary>What the project references, so rules only suggest controls and resources the project can actually use.</summary>
public sealed class ProjectContext
{
    private static readonly Regex ToolkitXmlns = new Regex(@"using:Uno\.Toolkit\.UI\b", RegexOptions.Compiled);
    private static readonly Regex NavigationXmlns = new Regex(@"using:Uno\.Extensions\.Navigation(\.UI)?\b", RegexOptions.Compiled);
    private static readonly Regex MvuxXmlns = new Regex(@"using:Uno\.Extensions\.Reactive(\.UI)?\b", RegexOptions.Compiled);
    private static readonly Regex ThemesXmlns = new Regex(@"using:Uno\.(Material|Cupertino|Simple|Themes)\b", RegexOptions.Compiled);
    private static readonly Regex UnoFeatures = new Regex(@"<UnoFeatures>([^<]*)</UnoFeatures>", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex ToolkitPackage = new Regex(@"PackageReference\s+Include\s*=\s*""Uno\.Toolkit", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex NavigationPackage = new Regex(@"PackageReference\s+Include\s*=\s*""Uno\.Extensions\.Navigation", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex MvuxPackage = new Regex(@"PackageReference\s+Include\s*=\s*""Uno\.Extensions\.Reactive", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex ThemesPackage = new Regex(@"PackageReference\s+Include\s*=\s*""Uno\.(Material|Cupertino|Simple|Themes)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex FeatureSplit = new Regex(@"[;\s]+", RegexOptions.Compiled);

    public bool HasToolkit { get; set; }

    public bool HasExtensionsNavigation { get; set; }

    /// <summary>Uno.Extensions.Reactive (MVUX) is referenced; enables the MVUX binding-trap rules.</summary>
    public bool HasMvux { get; set; }

    /// <summary>Uno.Themes (Material, Cupertino or SimpleTheme) is referenced, so role brushes like PrimaryBrush exist.</summary>
    public bool HasUnoThemes { get; set; }

    /// <summary>True when every flag is already set, so no further evidence can change the result.</summary>
    public bool IsComplete => HasToolkit && HasExtensionsNavigation && HasMvux && HasUnoThemes;

    public static ProjectContext Detect(IEnumerable<string> projectFileTexts, IEnumerable<string> xamlTexts)
    {
        var ctx = new ProjectContext();
        ctx.AddProjectFiles(projectFileTexts);
        ctx.AddXaml(xamlTexts);
        return ctx;
    }

    public void AddProjectFiles(IEnumerable<string> projectFileTexts)
    {
        foreach (var text in projectFileTexts)
        {
            HasToolkit |= ToolkitPackage.IsMatch(text);
            HasExtensionsNavigation |= NavigationPackage.IsMatch(text);
            HasMvux |= MvuxPackage.IsMatch(text);
            HasUnoThemes |= ThemesPackage.IsMatch(text);

            foreach (Match m in UnoFeatures.Matches(text))
            {
                foreach (var feature in FeatureSplit.Split(m.Groups[1].Value))
                {
                    switch (feature.Trim().ToLowerInvariant())
                    {
                        case "toolkit":
                        case "materialtoolkit":
                        case "cupertinotoolkit":
                            HasToolkit = true;
                            break;
                        case "navigation":
                            HasExtensionsNavigation = true;
                            break;
                        case "mvux":
                            HasMvux = true;
                            break;
                        case "material":
                        case "cupertino":
                        case "simpletheme":
                        case "themes":
                            HasUnoThemes = true;
                            break;
                    }

                    // Composite features such as MaterialToolkit also bring the theme.
                    if (feature.IndexOf("Material", StringComparison.OrdinalIgnoreCase) >= 0 || feature.IndexOf("Cupertino", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        HasUnoThemes = true;
                    }
                }
            }
        }
    }

    public void AddXaml(IEnumerable<string> xamlTexts)
    {
        foreach (var text in xamlTexts)
        {
            HasToolkit |= ToolkitXmlns.IsMatch(text);
            HasExtensionsNavigation |= NavigationXmlns.IsMatch(text);
            HasMvux |= MvuxXmlns.IsMatch(text);
            HasUnoThemes |= ThemesXmlns.IsMatch(text);

            if (IsComplete)
            {
                break;
            }
        }
    }

    /// <summary>Compact form for the on-disk cache: one character per flag.</summary>
    public string Serialize() => $"{(HasToolkit ? 'T' : '-')}{(HasExtensionsNavigation ? 'N' : '-')}{(HasMvux ? 'M' : '-')}{(HasUnoThemes ? 'H' : '-')}";

    public static bool TryDeserialize(string? text, out ProjectContext context)
    {
        context = new ProjectContext();
        if (text == null || text.Length != 4)
        {
            return false;
        }

        context.HasToolkit = text[0] == 'T';
        context.HasExtensionsNavigation = text[1] == 'N';
        context.HasMvux = text[2] == 'M';
        context.HasUnoThemes = text[3] == 'H';
        return true;
    }
}
