using System.Text.RegularExpressions;

namespace Uno.Lint;

/// <summary>What the project references, so rules only suggest controls the project can actually use.</summary>
public sealed class ProjectContext
{
    private static readonly Regex ToolkitXmlns = new Regex(@"using:Uno\.Toolkit\.UI\b", RegexOptions.Compiled);
    private static readonly Regex NavigationXmlns = new Regex(@"using:Uno\.Extensions\.Navigation(\.UI)?\b", RegexOptions.Compiled);
    private static readonly Regex UnoFeatures = new Regex(@"<UnoFeatures>([^<]*)</UnoFeatures>", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex ToolkitPackage = new Regex(@"PackageReference\s+Include\s*=\s*""Uno\.Toolkit", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex NavigationPackage = new Regex(@"PackageReference\s+Include\s*=\s*""Uno\.Extensions\.Navigation", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public bool HasToolkit { get; set; }

    public bool HasExtensionsNavigation { get; set; }

    public static ProjectContext Detect(IEnumerable<string> projectFileTexts, IEnumerable<string> xamlTexts)
    {
        var ctx = new ProjectContext();

        foreach (var text in projectFileTexts)
        {
            if (ToolkitPackage.IsMatch(text))
            {
                ctx.HasToolkit = true;
            }

            if (NavigationPackage.IsMatch(text))
            {
                ctx.HasExtensionsNavigation = true;
            }

            foreach (Match m in UnoFeatures.Matches(text))
            {
                var features = m.Groups[1].Value;
                if (features.IndexOf("Toolkit", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    ctx.HasToolkit = true;
                }

                if (features.IndexOf("Navigation", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    ctx.HasExtensionsNavigation = true;
                }
            }
        }

        foreach (var text in xamlTexts)
        {
            if (!ctx.HasToolkit && ToolkitXmlns.IsMatch(text))
            {
                ctx.HasToolkit = true;
            }

            if (!ctx.HasExtensionsNavigation && NavigationXmlns.IsMatch(text))
            {
                ctx.HasExtensionsNavigation = true;
            }

            if (ctx.HasToolkit && ctx.HasExtensionsNavigation)
            {
                break;
            }
        }

        return ctx;
    }
}
